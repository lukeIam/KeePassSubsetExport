using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KeePass;
using KeePass.Resources;
using KeePassLib;
using KeePassLib.Collections;
using KeePassLib.Cryptography.KeyDerivation;
using KeePassLib.Interfaces;
using KeePassLib.Keys;
using KeePassLib.Security;
using KeePassLib.Serialization;
using KeePassLib.Utility;

namespace KeePassSubsetExport
{
    internal static class Exporter
    {
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
            IEnumerable<PwEntry> jobSettings = settingsGroup.Entries
                .Where(x => x.Strings.ReadSafe("Title").Contains("SubsetExport_"));

            // Loop through all found entries - each on is a export job 
            foreach (var settingsEntry in jobSettings)
            {
                // Load settings for this job
                var settings = Settings.Parse(settingsEntry);

                // If a key file is given it must exist.
                if (!string.IsNullOrEmpty(settings.KeyFilePath) && !File.Exists(settings.KeyFilePath))
                {
                    MessageService.ShowWarning("SubsetExport: Keyfile is given but could not be found for: " +
                                               settingsEntry.Strings.ReadSafe("Title"), settings.KeyFilePath);
                    continue;
                }

                // If keyTransformationRounds are given it must be an integer.
                ulong keyTransformationRounds = 0;
                if (!string.IsNullOrEmpty(settings.KeyTransformationRoundsString) && !ulong.TryParse(settings.KeyTransformationRoundsString.Trim(), out keyTransformationRounds))
                {
                    MessageService.ShowWarning("SubsetExport: keyTransformationRounds is given but can not be parsed as integer for: " +
                                               settingsEntry.Strings.ReadSafe("Title"), settings.KeyFilePath);
                    continue;
                }
                settings.KeyTransformationRounds = keyTransformationRounds;

                // Require at least one of Tag or Group
                if (string.IsNullOrEmpty(settings.Tag) && string.IsNullOrEmpty(settings.Group))
                {
                    MessageService.ShowWarning("SubsetExport: Missing Tag or Group for: " +
                                               settingsEntry.Strings.ReadSafe("Title"));
                    continue;
                }

                // Require targetFilePath and at least one of password or keyFilePath.
                if (string.IsNullOrEmpty(settings.TargetFilePath))
                {
                    MessageService.ShowWarning("SubsetExport: Missing TargetFilePath for: " +
                                               settingsEntry.Strings.ReadSafe("Title"));
                    continue;
                }

                // Require at least one of Password or KeyFilePath.
                if (settings.Password.IsEmpty && !File.Exists(settings.KeyFilePath))
                {
                    MessageService.ShowWarning("SubsetExport: Missing Password or valid KeyFilePath for: " +
                                               settingsEntry.Strings.ReadSafe("Title"));
                    continue;
                }

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

        /// <summary>
        /// Exports all entries with the given tag to a new database at the given path.
        /// </summary>
        /// <param name="sourceDb">The source database.</param>
        /// <param name="settings">The settings for this job.</param>
        private static void CopyToNewDb(PwDatabase sourceDb, Settings settings)
        {
            // Create a key for the target database
            CompositeKey key = new CompositeKey();

            bool hasPassword = false;
            bool hasKeyFile = false;

            if (!settings.Password.IsEmpty)
            {
                byte[] passwordByteArray = settings.Password.ReadUtf8();
                key.AddUserKey(new KcpPassword(passwordByteArray));
                MemUtil.ZeroByteArray(passwordByteArray);
                hasPassword = true;
            }

            // Load a keyfile for the target database if requested (and add it to the key)
            if (!string.IsNullOrEmpty(settings.KeyFilePath))
            {
                bool bIsKeyProv = Program.KeyProviderPool.IsKeyProvider(settings.KeyFilePath);

                if (!bIsKeyProv)
                {
                    try
                    {
                        key.AddUserKey(new KcpKeyFile(settings.KeyFilePath, true));
                        hasKeyFile = true;
                    }
                    catch (InvalidDataException exId)
                    {
                        MessageService.ShowWarning(settings.KeyFilePath, exId);
                    }
                    catch (Exception exKf)
                    {
                        MessageService.ShowWarning(settings.KeyFilePath, KPRes.KeyFileError, exKf);
                    }
                }
                else
                {
                    KeyProviderQueryContext ctxKp = new KeyProviderQueryContext(
                        ConnectionInfo, true, false);

                    KeyProvider prov = Program.KeyProviderPool.Get(settings.KeyFilePath);
                    bool bPerformHash = !prov.DirectKey;
                    byte[] pbCustomKey = prov.GetKey(ctxKp);

                    if ((pbCustomKey != null) && (pbCustomKey.Length > 0))
                    {
                        try
                        {
                            key.AddUserKey(new KcpCustomKey(settings.KeyFilePath, pbCustomKey, bPerformHash));
                            hasKeyFile = true;
                        }
                        catch (Exception exCkp)
                        {
                            MessageService.ShowWarning(exCkp);
                        }

                        MemUtil.ZeroByteArray(pbCustomKey);
                    }
                }
            }

            // Check if at least a password or a keyfile have been added to the key object
            if (!hasPassword && !hasKeyFile)
            {
                // Fail if not
                throw new InvalidOperationException("For the target database at least a password or a keyfile is required.");
            }

            // Create a new database 
            PwDatabase targetDatabase = new PwDatabase();

            // Apply the created key to the new database
            targetDatabase.New(new IOConnectionInfo(), key);

            // Copy database settings
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
            
            if (settings.KeyTransformationRounds == 0)
            {
                // keyTransformationRounds was not set -> use the one from the source database
                settings.KeyTransformationRounds = sourceDb.KdfParameters.GetUInt64(AesKdf.ParamRounds, 0);
            }

            // Set keyTransformationRounds (min PwDefs.DefaultKeyEncryptionRounds)
            targetDatabase.KdfParameters.SetUInt64(AesKdf.ParamRounds, Math.Max(PwDefs.DefaultKeyEncryptionRounds, settings.KeyTransformationRounds));
            
            // Assign the properties of the source root group to the target root group
            targetDatabase.RootGroup.AssignProperties(sourceDb.RootGroup, false, true);
            HandleCustomIcon(targetDatabase, sourceDb, sourceDb.RootGroup);

            // Overwrite the root group name if requested
            if (!string.IsNullOrEmpty(settings.RootGroupName))
            {
                targetDatabase.RootGroup.Name = settings.RootGroupName;
            }

            // Find all entries matching the tag
            PwObjectList<PwEntry> entries = new PwObjectList<PwEntry>();

            if (!string.IsNullOrEmpty(settings.Tag) && string.IsNullOrEmpty(settings.Group))
            {
                // Tag only export
                sourceDb.RootGroup.FindEntriesByTag(settings.Tag, entries, true);
            }
            else if (string.IsNullOrEmpty(settings.Tag) && !string.IsNullOrEmpty(settings.Group))
            {
                // Tag and group export
                PwGroup groupToExport = sourceDb.RootGroup.GetFlatGroupList().FirstOrDefault(g => g.Name == settings.Group);

                if (groupToExport == null)
                {
                    throw new ArgumentException("No group with the name of the Group-Setting found.");
                }

                entries = groupToExport.GetEntries(true);
            }
            else if (!string.IsNullOrEmpty(settings.Tag) && !string.IsNullOrEmpty(settings.Group))
            {
                // Tag and group export
                PwGroup groupToExport = sourceDb.RootGroup.GetFlatGroupList().FirstOrDefault(g => g.Name == settings.Group);
                
                if (groupToExport == null)
                {
                    throw new ArgumentException("No group with the name of the Group-Setting found.");
                }
                
                groupToExport.FindEntriesByTag(settings.Tag, entries, true);
            }
            else
            {
                throw new ArgumentException("At least one of Tag or ExportFolderName must be set.");
            }


            // Copy all entries to the new database
            foreach (PwEntry entry in entries)
            {
                // Get or create the target group in the target database (including hierarchy)
                PwGroup targetGroup = CreateTargetGroupInDatebase(entry, targetDatabase, sourceDb);

                // Clone entry
                PwEntry peNew = new PwEntry(false, false);
                peNew.Uuid = entry.Uuid;
                peNew.AssignProperties(entry, false, true, true);

                // Handle custom icon
                HandleCustomIcon(targetDatabase, sourceDb, entry);

                // Add entry to the target group in the new database
                targetGroup.AddEntry(peNew, true);
            }

            // Default to same folder as sourceDb for target if no directory is specified
            if (!Path.IsPathRooted(settings.TargetFilePath))
            {
                string sourceDbPath = Path.GetDirectoryName(sourceDb.IOConnectionInfo.Path);
                settings.TargetFilePath = Path.Combine(sourceDbPath, settings.TargetFilePath);
            }

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