using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KeePass;
using KeePass.Resources;
using KeePassLib;
using KeePassLib.Collections;
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
                ProtectedString password = settingsEntry.Strings.GetSafe("Password");
                string targetFilePath = settingsEntry.Strings.ReadSafe("SubsetExport_TargetFilePath");
                string keyFilePath = settingsEntry.Strings.ReadSafe("SubsetExport_KeyFilePath");
                string tag = settingsEntry.Strings.ReadSafe("SubsetExport_Tag");

                // If a key file is given it must exist.
                if (!string.IsNullOrEmpty(keyFilePath) && !File.Exists(keyFilePath))
                {
                    MessageService.ShowWarning("SubsetExport: Keyfile is given but could not be found for: " +
                                               settingsEntry.Strings.ReadSafe("Title"), keyFilePath);
                    continue;
                }

                // Require at least targetFilePath, tag and at least one of password or keyFilePath.
                if (string.IsNullOrEmpty(targetFilePath) || string.IsNullOrEmpty(tag) || (password.IsEmpty && !File.Exists(keyFilePath)))
                {
                    MessageService.ShowWarning("SubsetExport: Missing settings for: " +
                                               settingsEntry.Strings.ReadSafe("Title"));
                    continue;
                }

                try
                {
                    // Execute the export 
                    CopyToNewDb(sourceDb, targetFilePath, password, keyFilePath, tag);
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
        /// <param name="targetFilePath">The path for the target database.</param>
        /// <param name="password">The password to protect the target database(optional if <para>keyFilePath</para> is set).</param>
        /// <param name="keyFilePath">The path to a key file to protect the target database (optional if <para>password</para> is set).</param>
        /// <param name="tag"></param>
        private static void CopyToNewDb(PwDatabase sourceDb, string targetFilePath, ProtectedString password, string keyFilePath, string tag)
        {
            // Create a key for the target database
            CompositeKey key = new CompositeKey();

            bool hasPassword = false;
            bool hasKeyFile = false;

            if (!password.IsEmpty)
            {
                byte[] passwordByteArray = password.ReadUtf8();
                key.AddUserKey(new KcpPassword(passwordByteArray));
                MemUtil.ZeroByteArray(passwordByteArray);
                hasPassword = true;
            }

            // Load a keyfile for the target database if requested (and add it to the key)
            if (!string.IsNullOrEmpty(keyFilePath))
            {
                bool bIsKeyProv = Program.KeyProviderPool.IsKeyProvider(keyFilePath);

                if (!bIsKeyProv)
                {
                    try
                    {
                        key.AddUserKey(new KcpKeyFile(keyFilePath, true));
                        hasKeyFile = true;
                    }
                    catch (InvalidDataException exId)
                    {
                        MessageService.ShowWarning(keyFilePath, exId);
                    }
                    catch (Exception exKf)
                    {
                        MessageService.ShowWarning(keyFilePath, KPRes.KeyFileError, exKf);
                    }
                }
                else
                {
                    KeyProviderQueryContext ctxKp = new KeyProviderQueryContext(
                        ConnectionInfo, true, false);

                    KeyProvider prov = Program.KeyProviderPool.Get(keyFilePath);
                    bool bPerformHash = !prov.DirectKey;
                    byte[] pbCustomKey = prov.GetKey(ctxKp);

                    if ((pbCustomKey != null) && (pbCustomKey.Length > 0))
                    {
                        try
                        {
                            key.AddUserKey(new KcpCustomKey(keyFilePath, pbCustomKey, bPerformHash));
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

            // Copy the root group name
            targetDatabase.RootGroup.Name = sourceDb.RootGroup.Name;

            // Find all entries matching the tag
            PwObjectList<PwEntry> entries = new PwObjectList<PwEntry>();
            sourceDb.RootGroup.FindEntriesByTag(tag, entries, true);

            // Copy all entries to the new database
            foreach (PwEntry entry in entries)
            {
                // Get or create the target group in the target database (including hierarchy)
                PwGroup targetGroup = CreateTargetGroupInDatebase(entry, targetDatabase);

                // Clone entry
                PwEntry peNew = new PwEntry(false, false);
                peNew.Uuid = entry.Uuid;
                peNew.AssignProperties(entry, false, true, true);

                // Does the entry use a custom icon and its not in already in the target database
                if (!entry.CustomIconUuid.Equals(PwUuid.Zero)  && targetDatabase.GetCustomIconIndex(entry.CustomIconUuid) == -1)
                {
                    // Check if the custom icon really is in the source database
                    int iconIndex = sourceDb.GetCustomIconIndex(entry.CustomIconUuid);
                    if (iconIndex < 0 || iconIndex > sourceDb.CustomIcons.Count - 1)
                    {
                        MessageService.ShowWarning("Can't locate custom icon (" + entry.CustomIconUuid.ToHexString() + ") for entry " + entry.Strings.ReadSafe("Title"));
                        continue;
                    }

                    // Get the custom icon from the source database
                    PwCustomIcon customIcon = sourceDb.CustomIcons[iconIndex];

                    // Copy the custom icon to the target database
                    targetDatabase.CustomIcons.Add(customIcon);
                }

                // Add entry to the target group in the new database
                targetGroup.AddEntry(peNew, true);
            }

            // Create target folder (if not exist)
            string targetFolder = Path.GetDirectoryName(targetFilePath);

            if (targetFolder == null)
            {
                throw new ArgumentException("Can't get target folder.");
            }
            Directory.CreateDirectory(targetFolder);

            // Save the new database under the target path
            KdbxFile kdbx = new KdbxFile(targetDatabase);

            using (FileStream outputStream = new FileStream(targetFilePath, FileMode.Create))
            {
                kdbx.Save(outputStream, null, KdbxFormat.Default, new NullStatusLogger());
            }
        }

        /// <summary>
        /// Get or create the target group of an entry in the target database (including hierarchy).
        /// </summary>
        /// <param name="entry">An entry wich is located in the folder with the target structure.</param>
        /// <param name="targetDatabase">The target database in which the folder structure should be created.</param>
        /// <returns>The target folder in the target database.</returns>
        private static PwGroup CreateTargetGroupInDatebase(PwEntry entry, PwDatabase targetDatabase)
        {
            // Collect all group names from the entry up to the root group
            PwGroup group = entry.ParentGroup;
            List<string> list = new List<string>();

            while (group != null)
            {
                list.Add(group.Name);
                group = group.ParentGroup;
            }

            // Remove root group (we already changed the root group name)
            list.RemoveAt(list.Count - 1);
            // groups are in a bottom-up oder -> reverse to get top-down
            list.Reverse();

            // Create a string representing the folder structure for FindCreateSubTree()
            string groupPath = string.Join("/", list.ToArray());

            // Find the leaf folder or create it including hierarchical folder structure
            PwGroup targetGroup = targetDatabase.RootGroup.FindCreateSubTree(groupPath, new char[]
            {
                '/'
            });

            // Return the target folder (leaf folder)
            return targetGroup;
        }
    }
}