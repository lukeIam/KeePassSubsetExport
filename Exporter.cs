using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KeePassLib;
using KeePassLib.Collections;
using KeePassLib.Cryptography.KeyDerivation;
using KeePassLib.Interfaces;
using KeePassLib.Keys;
using KeePassLib.Serialization;
using KeePassLib.Utility;

namespace KeePassSubsetExport
{
    internal static class Exporter
    {
        private static readonly PwUuid UuidAes = new PwUuid(new byte[] {
            0xC9, 0xD9, 0xF3, 0x9A, 0x62, 0x8A, 0x44, 0x60,
            0xBF, 0x74, 0x0D, 0x08, 0xC1, 0x8A, 0x4F, 0xEA });

        private static readonly PwUuid UuidArgon2 = new PwUuid(new byte[] {
            0xEF, 0x63, 0x6D, 0xDF, 0x8C, 0x29, 0x44, 0x4B,
            0x91, 0xF7, 0xA9, 0xA4, 0x03, 0xE3, 0x0A, 0x0C });

        private static readonly IOConnectionInfo ConnectionInfo = new IOConnectionInfo();

        /// <summary>
        /// Exports all entries with the given tag to a new database at the given path (multiple jobs possible).
        /// Each job is an entry in the "SubsetExportSettings" folder with a title "SubsetExport_*".
        /// Password == password field of the entry
        /// keyFilePath == "SubsetExport_KeyFilePath" string field on the entry
        /// targetFilePath == "SubsetExport_TargetFilePath" string field on the entry
        /// tag (filter) == "SubsetExport_Tag" string field on the entry
        /// </summary>
        /// <param name="sourceDb">The source database to run the exports on.</param>
        internal static void Export(PwDatabase sourceDb)
        {
            // Get all entries out of the group "SubsetExportSettings" which start with "SubsetExport_"
            PwGroup settingsGroup = sourceDb.RootGroup.Groups.FirstOrDefault(g => g.Name == "SubsetExportSettings");
            if (settingsGroup == null)
            {
                return;
            }
            IEnumerable<PwEntry> jobSettings = settingsGroup.Entries;

            // Loop through all found entries - each on is a export job 
            foreach (var settingsEntry in jobSettings)
            {
                // Load settings for this job
                var settings = Settings.Parse(settingsEntry);

                if (CheckKeyFile(sourceDb, settings, settingsEntry))
                    continue;

                if (CheckTagOrGroup(settings, settingsEntry))
                    continue;

                if (CheckTargetFilePath(settings, settingsEntry))
                    continue;

                if (CheckPasswordOrKeyfile(settings, settingsEntry))
                    continue;

                try
                {
                    // Execute the export 
                    CopyToNewDb(sourceDb, settings);
                }
                catch (Exception e)
                {
                    MessageService.ShowWarning("SubsetExport failed:", e);
                }
            }
        }

        private static bool CheckPasswordOrKeyfile(Settings settings, PwEntry settingsEntry)
        {
            // Require at least one of Password or KeyFilePath.
            if (settings.Password.IsEmpty && !File.Exists(settings.KeyFilePath))
            {
                MessageService.ShowWarning("SubsetExport: Missing Password or valid KeyFilePath for: " +
                                           settingsEntry.Strings.ReadSafe("Title"));
                return true;
            }

            return false;
        }

        private static bool CheckTargetFilePath(Settings settings, PwEntry settingsEntry)
        {
            // Require targetFilePath
            if (string.IsNullOrEmpty(settings.TargetFilePath))
            {
                MessageService.ShowWarning("SubsetExport: Missing TargetFilePath for: " +
                                           settingsEntry.Strings.ReadSafe("Title"));
                return true;
            }

            return false;
        }

        private static bool CheckTagOrGroup(Settings settings, PwEntry settingsEntry)
        {
            // Require at least one of Tag or Group
            if (string.IsNullOrEmpty(settings.Tag) && string.IsNullOrEmpty(settings.Group))
            {
                MessageService.ShowWarning("SubsetExport: Missing Tag or Group for: " +
                                           settingsEntry.Strings.ReadSafe("Title"));
                return true;
            }

            return false;
        }

        private static Boolean CheckKeyFile(PwDatabase sourceDb, Settings settings, PwEntry settingsEntry)
        {
            // If a key file is given it must exist.
            if (!string.IsNullOrEmpty(settings.KeyFilePath))
            {
                // Default to same folder as sourceDb for the keyfile if no directory is specified
                if (!Path.IsPathRooted(settings.KeyFilePath))
                {
                    string sourceDbPath = Path.GetDirectoryName(sourceDb.IOConnectionInfo.Path);
                    if (sourceDbPath != null)
                    {
                        settings.KeyFilePath = Path.Combine(sourceDbPath, settings.KeyFilePath);
                    }
                }

                if (!File.Exists(settings.KeyFilePath))
                {
                    MessageService.ShowWarning("SubsetExport: Keyfile is given but could not be found for: " +
                                               settingsEntry.Strings.ReadSafe("Title"), settings.KeyFilePath);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Exports all entries with the given tag to a new database at the given path.
        /// </summary>
        /// <param name="sourceDb">The source database.</param>
        /// <param name="settings">The settings for this job.</param>
        private static void CopyToNewDb(PwDatabase sourceDb, Settings settings)
        {
            // Create a key for the target database
            CompositeKey key = CreateCompositeKey(settings);

            // Create or open the target database
            PwDatabase targetDatabase = CreateTargetDatabase(sourceDb, settings, key);

            // Copy database settings
            CopyDatabaseSettings(sourceDb, targetDatabase);

            // Copy key derivation function parameters
            CopyKdfSettings(sourceDb, settings, targetDatabase);

            // Assign the properties of the source root group to the target root group
            targetDatabase.RootGroup.AssignProperties(sourceDb.RootGroup, false, true);
            HandleCustomIcon(targetDatabase, sourceDb, sourceDb.RootGroup);

            // Overwrite the root group name if requested
            if (!string.IsNullOrEmpty(settings.RootGroupName))
            {
                targetDatabase.RootGroup.Name = settings.RootGroupName;
            }

            // Find all entries matching the tag
            PwObjectList<PwEntry> entries = GetMatching(sourceDb, settings);

            // Copy all entries to the new database
            CopyEntriesAndGroups(sourceDb, settings, entries, targetDatabase);

            // Save new database
            SaveTargetDatabase(settings, targetDatabase);
        }

        private static PwObjectList<PwEntry> GetMatching(PwDatabase sourceDb, Settings settings)
        {
            PwObjectList<PwEntry> entries = new PwObjectList<PwEntry>();

            if (!string.IsNullOrEmpty(settings.Tag) && string.IsNullOrEmpty(settings.Group))
            {
                // Tag only export
                // Support multiple tag (Tag1,Tag2)
                foreach (string tag in settings.Tag.Split(','))
                {
                    PwObjectList<PwEntry> tagEntries = new PwObjectList<PwEntry>();
                    sourceDb.RootGroup.FindEntriesByTag(tag, tagEntries, true);
                    // Prevent duplicated entries
                    IEnumerable<PwUuid> existingUuids = entries.Select(x => x.Uuid);
                    List<PwEntry> entriesToAdd = tagEntries.Where(x => existingUuids.Contains(x.Uuid)).ToList();
                    entries.Add(entriesToAdd);
                }
            }
            else if (string.IsNullOrEmpty(settings.Tag) && !string.IsNullOrEmpty(settings.Group))
            {
                // Support multiple group (Group1,Group2)
                foreach (string group in settings.Group.Split(','))
                {
                    // Tag and group export
                    PwGroup groupToExport = sourceDb.RootGroup.GetFlatGroupList().FirstOrDefault(g => g.Name == group);

                    if (groupToExport == null)
                    {
                        throw new ArgumentException("No group with the name of the Group-Setting found.");
                    }

                    PwObjectList<PwEntry> groupEntries = groupToExport.GetEntries(true);
                    // Prevent duplicated entries
                    IEnumerable<PwUuid> existingUuids = entries.Select(x => x.Uuid);
                    List<PwEntry> entriesToAdd = groupEntries.Where(x => existingUuids.Contains(x.Uuid)).ToList();
                    entries.Add(entriesToAdd);
                }
            }
            else if (!string.IsNullOrEmpty(settings.Tag) && !string.IsNullOrEmpty(settings.Group))
            {
                // Tag and group export
                foreach (string group in settings.Group.Split(','))
                {
                    PwGroup groupToExport = sourceDb.RootGroup.GetFlatGroupList().FirstOrDefault(g => g.Name == group);

                    if (groupToExport == null)
                    {
                        throw new ArgumentException("No group with the name of the Group-Setting found.");
                    }

                    foreach (string tag in settings.Tag.Split(','))
                    {
                        PwObjectList<PwEntry> tagEntries = new PwObjectList<PwEntry>();
                        groupToExport.FindEntriesByTag(tag, tagEntries, true);
                        entries.Add(tagEntries);
                        // Prevent duplicated entries
                        IEnumerable<PwUuid> existingUuids = entries.Select(x => x.Uuid);
                        List<PwEntry> entriesToAdd = tagEntries.Where(x => existingUuids.Contains(x.Uuid)).ToList();
                        entries.Add(entriesToAdd);
                    }
                }
            }
            else
            {
                throw new ArgumentException("At least one of Tag or ExportFolderName must be set.");
            }

            return entries;
        }

        private static void CopyEntriesAndGroups(PwDatabase sourceDb, Settings settings, PwObjectList<PwEntry> entries,
            PwDatabase targetDatabase)
        {
            foreach (PwEntry entry in entries)
            {
                // Get or create the target group in the target database (including hierarchy)
                PwGroup targetGroup = settings.FlatExport
                    ? targetDatabase.RootGroup
                    : CreateTargetGroupInDatebase(entry, targetDatabase, sourceDb);

                PwEntry peNew = null;
                if (!settings.OverrideTargetDatabase)
                {
                    peNew = targetGroup.FindEntry(entry.Uuid, bSearchRecursive: false);

                    // Check if the target entry is newer than the source entry
                    if (settings.OverrideEntryOnlyNewer && peNew != null &&
                        peNew.LastModificationTime > entry.LastModificationTime)
                    {
                        // Yes -> skip this entry
                        continue;
                    }
                }

                // Was no existing entry in the target database found?
                if (peNew == null)
                {
                    // Create a new entry
                    peNew = new PwEntry(false, false);
                    peNew.Uuid = entry.Uuid;

                    // Add entry to the target group in the new database
                    targetGroup.AddEntry(peNew, true);
                }

                // Clone entry properties
                peNew.AssignProperties(entry, false, true, true);

                // Handle custom icon
                HandleCustomIcon(targetDatabase, sourceDb, entry);
            }
        }

        private static void SaveTargetDatabase(Settings settings, PwDatabase targetDatabase)
        {
            if (!settings.OverrideTargetDatabase && File.Exists(settings.TargetFilePath))
            {
                // Save changes to existing target database
                targetDatabase.Save(new NullStatusLogger());
            }
            else
            {
                // Create target folder (if not exist)
                string targetFolder = Path.GetDirectoryName(settings.TargetFilePath);

                if (targetFolder == null)
                {
                    throw new ArgumentException("Can't get target folder.");
                }

                Directory.CreateDirectory(targetFolder);

                // Save the new database under the target path
                KdbxFile kdbx = new KdbxFile(targetDatabase);

                using (FileStream outputStream = new FileStream(settings.TargetFilePath, FileMode.Create))
                {
                    kdbx.Save(outputStream, null, KdbxFormat.Default, new NullStatusLogger());
                }
            }
        }

        private static void CopyKdfSettings(PwDatabase sourceDb, Settings settings, PwDatabase targetDatabase)
        {
            // Create a clone of the KdfParameters object. As cloning is not supportet serialize and deserialize
            targetDatabase.KdfParameters = KdfParameters.DeserializeExt(KdfParameters.SerializeExt(sourceDb.KdfParameters));

            if (Equals(targetDatabase.KdfParameters.KdfUuid, UuidAes))
            {
                // Allow override of AesKdf transformation rounds
                if (settings.KeyTransformationRounds != 0)
                {
                    // Set keyTransformationRounds (min PwDefs.DefaultKeyEncryptionRounds)
                    targetDatabase.KdfParameters.SetUInt64(AesKdf.ParamRounds,
                        Math.Max(PwDefs.DefaultKeyEncryptionRounds, settings.KeyTransformationRounds));
                }
            }
            else if (Equals(targetDatabase.KdfParameters.KdfUuid, UuidArgon2))
            {
                // Allow override of Agon2Kdf transformation rounds
                if (settings.Argon2ParamIterations != 0)
                {
                    // Set paramIterations (min default value == 2)
                    targetDatabase.KdfParameters.SetUInt64(Argon2Kdf.ParamIterations,
                        Math.Max(2, settings.Argon2ParamIterations));
                }

                // Allow override of Agon2Kdf memory setting
                if (settings.Argon2ParamMemory != 0)
                {
                    // Set ParamMemory (min default value == 1048576 == 1 MB)
                    targetDatabase.KdfParameters.SetUInt64(Argon2Kdf.ParamMemory,
                        Math.Max(1048576, settings.Argon2ParamMemory));
                }

                // Allow override of Agon2Kdf parallelism setting
                if (settings.Argon2ParamParallelism != 0)
                {
                    // Set ParamParallelism (min default value == 2 MB)
                    targetDatabase.KdfParameters.SetUInt32(Argon2Kdf.ParamParallelism, settings.Argon2ParamParallelism);
                }
            }
        }

        private static void CopyDatabaseSettings(PwDatabase sourceDb, PwDatabase targetDatabase)
        {
            targetDatabase.Color = sourceDb.Color;
            targetDatabase.Compression = sourceDb.Compression;
            targetDatabase.DataCipherUuid = sourceDb.DataCipherUuid;
            targetDatabase.DefaultUserName = sourceDb.DefaultUserName;
            targetDatabase.Description = sourceDb.Description;
            targetDatabase.HistoryMaxItems = sourceDb.HistoryMaxItems;
            targetDatabase.HistoryMaxSize = sourceDb.HistoryMaxSize;
            targetDatabase.MaintenanceHistoryDays = sourceDb.MaintenanceHistoryDays;
            targetDatabase.MasterKeyChangeForce = sourceDb.MasterKeyChangeForce;
            targetDatabase.MasterKeyChangeRec = sourceDb.MasterKeyChangeRec;
            targetDatabase.Name = sourceDb.Name;
            targetDatabase.RecycleBinEnabled = sourceDb.RecycleBinEnabled;
        }

        private static PwDatabase CreateTargetDatabase(PwDatabase sourceDb, Settings settings, CompositeKey key)
        {
            // Default to same folder as sourceDb for target if no directory is specified
            if (!Path.IsPathRooted(settings.TargetFilePath))
            {
                string sourceDbPath = Path.GetDirectoryName(sourceDb.IOConnectionInfo.Path);
                if (sourceDbPath != null)
                {
                    settings.TargetFilePath = Path.Combine(sourceDbPath, settings.TargetFilePath);
                }
            }

            // Create a new database 
            PwDatabase targetDatabase = new PwDatabase();

            if (!settings.OverrideTargetDatabase && File.Exists(settings.TargetFilePath))
            {
                // Connect the database object to the existing database
                targetDatabase.Open(new IOConnectionInfo()
                {
                    Path = settings.TargetFilePath
                }, key, new NullStatusLogger());
            }
            else
            {
                // Apply the created key to the new database
                targetDatabase.New(new IOConnectionInfo(), key);
            }

            return targetDatabase;
        }

        private static CompositeKey CreateCompositeKey(Settings settings)
        {
            CompositeKey key = new CompositeKey();

            bool hasPassword = false;
            bool hasKeyFile = false;

            if (!settings.Password.IsEmpty)
            {
                byte[] passwordByteArray = settings.Password.ReadUtf8();
                hasPassword = KeyHelper.AddPasswordToKey(passwordByteArray, key);
                MemUtil.ZeroByteArray(passwordByteArray);
            }

            // Load a keyfile for the target database if requested (and add it to the key)
            if (!string.IsNullOrEmpty(settings.KeyFilePath))
            {
                hasKeyFile = KeyHelper.AddKeyfileToKey(settings.KeyFilePath, key, ConnectionInfo);
            }

            // Check if at least a password or a keyfile have been added to the key object
            if (!hasPassword && !hasKeyFile)
            {
                // Fail if not
                throw new InvalidOperationException("For the target database at least a password or a keyfile is required.");
            }

            return key;
        }

        /// <summary>
        /// Get or create the target group of an entry in the target database (including hierarchy).
        /// </summary>
        /// <param name="entry">An entry wich is located in the folder with the target structure.</param>
        /// <param name="targetDatabase">The target database in which the folder structure should be created.</param>
        /// <param name="sourceDatabase">The source database from which the folder properties should be taken.</param>
        /// <returns>The target folder in the target database.</returns>
        private static PwGroup CreateTargetGroupInDatebase(PwEntry entry, PwDatabase targetDatabase, PwDatabase sourceDatabase)
        {
            // Collect all group names from the entry up to the root group
            PwGroup group = entry.ParentGroup;
            List<PwUuid> list = new List<PwUuid>();

            while (group != null)
            {
                list.Add(group.Uuid);
                group = group.ParentGroup;
            }

            // Remove root group (we already changed the root group name)
            list.RemoveAt(list.Count - 1);
            // groups are in a bottom-up oder -> reverse to get top-down
            list.Reverse();

            // Create group structure for the new entry (copying group properties)
            PwGroup lastGroup = targetDatabase.RootGroup;
            foreach (PwUuid id in list)
            {
                // Does the target group already exist?
                PwGroup newGroup = lastGroup.FindGroup(id, false);
                if (newGroup != null)
                {
                    lastGroup = newGroup;
                    continue;
                }

                // Get the source group
                PwGroup sourceGroup = sourceDatabase.RootGroup.FindGroup(id, true);

                // Create a new group and asign all properties from the source group
                newGroup = new PwGroup();
                newGroup.AssignProperties(sourceGroup, false, true);
                HandleCustomIcon(targetDatabase, sourceDatabase, sourceGroup);

                // Add the new group at the right position in the target database
                lastGroup.AddGroup(newGroup, true);

                lastGroup = newGroup;
            }

            // Return the target folder (leaf folder)
            return lastGroup;
        }

        /// <summary>
        /// Copies the custom icons required for this group to the target database.
        /// </summary>
        /// <param name="targetDatabase">The target database where to add the icons.</param>
        /// <param name="sourceDatabase">The source database where to get the icons from.</param>
        /// <param name="sourceGroup">The source group which icon should be copied (if it is custom).</param>
        private static void HandleCustomIcon(PwDatabase targetDatabase, PwDatabase sourceDatabase, PwGroup sourceGroup)
        {
            // Does the group not use a custom icon or is it already in the target database
            if (sourceGroup.CustomIconUuid.Equals(PwUuid.Zero) ||
                targetDatabase.GetCustomIconIndex(sourceGroup.CustomIconUuid) != -1)
            {
                return;
            }

            // Check if the custom icon really is in the source database
            int iconIndex = sourceDatabase.GetCustomIconIndex(sourceGroup.CustomIconUuid);
            if (iconIndex < 0 || iconIndex > sourceDatabase.CustomIcons.Count - 1)
            {
                MessageService.ShowWarning("Can't locate custom icon (" + sourceGroup.CustomIconUuid.ToHexString() +
                                           ") for group " + sourceGroup.Name);
            }

            // Get the custom icon from the source database
            PwCustomIcon customIcon = sourceDatabase.CustomIcons[iconIndex];

            // Copy the custom icon to the target database
            targetDatabase.CustomIcons.Add(customIcon);
        }

        /// <summary>
        /// Copies the custom icons required for this group to the target database.
        /// </summary>
        /// <param name="targetDatabase">The target database where to add the icons.</param>
        /// <param name="sourceDb">The source database where to get the icons from.</param>
        /// <param name="entry">The entry which icon should be copied (if it is custom).</param>
        private static void HandleCustomIcon(PwDatabase targetDatabase, PwDatabase sourceDb, PwEntry entry)
        {
            // Does the entry not use a custom icon or is it already in the target database
            if (entry.CustomIconUuid.Equals(PwUuid.Zero) ||
                targetDatabase.GetCustomIconIndex(entry.CustomIconUuid) != -1)
            {
                return;
            }

            // Check if the custom icon really is in the source database
            int iconIndex = sourceDb.GetCustomIconIndex(entry.CustomIconUuid);
            if (iconIndex < 0 || iconIndex > sourceDb.CustomIcons.Count - 1)
            {
                MessageService.ShowWarning("Can't locate custom icon (" + entry.CustomIconUuid.ToHexString() +
                                           ") for entry " + entry.Strings.ReadSafe("Title"));
            }

            // Get the custom icon from the source database
            PwCustomIcon customIcon = sourceDb.CustomIcons[iconIndex];

            // Copy the custom icon to the target database
            targetDatabase.CustomIcons.Add(customIcon);
        }
    }
}
