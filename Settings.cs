using System;
using KeePassLib;
using KeePassLib.Security;
using KeePassLib.Utility;

namespace KeePassSubsetExport
{
    /// <summary>
    /// Contains all settings for a job.
    /// </summary>
    public class Settings
    {
        /// <summary>
        /// The password to protect the target database(optional if <see cref="KeyFilePath"/> is set)
        /// </summary>
        public ProtectedString Password { get; private set; }
        /// <summary>
        /// The path for the target database.
        /// </summary>
        public string TargetFilePath { get; set; }
        /// <summary>
        /// The path to a key file to protect the target database (optional if <see cref="Password"/> is set).
        /// </summary>
        public string KeyFilePath { get; set; }
        /// <summary>
        /// Tag to export (optional if <see cref="Group"/> is set).
        /// </summary>
        public string Tag { get; private set; }
        /// <summary>
        /// The parsed KeyTransformationRounds for KdfAes.
        /// </summary>
        public ulong KeyTransformationRounds { get; set; }
        /// <summary>
        /// The parsed number of interations for KdfArgon2.
        /// </summary>
        public ulong Argon2ParamIterations { get; set; }
        /// <summary>
        /// The parsed memory amount for KdfArgon2.
        /// </summary>
        public ulong Argon2ParamMemory { get; set; }
        /// <summary>
        /// The parsed count of parallelism for KdfArgon2.
        /// </summary>
        public uint Argon2ParamParallelism { get; set; }
        /// <summary>
        /// The new name for the root group (optional).
        /// </summary>
        public string RootGroupName { get; private set; }
        /// <summary>
        /// The name of the group to export (optional if <see cref="Tag"/> is set).
        /// </summary>
        public string Group { get; private set; }
        /// <summary>
        /// If true, the export progress will ignore groups/folders, false otherwise (optional, defaults to false).
        /// </summary>
        public bool FlatExport { get; private set; }
        /// <summary>
        /// If true, the target database will be overriden, otherwise the entries will added to the target database (optional, defaults to true).
        /// </summary>
        public bool OverrideTargetDatabase { get; private set; }
        /// <summary>
        /// If true, only newer entries will overrides older entries (only works with <see cref="OverrideTargetDatabase"/> == false).
        /// </summary>
        public bool OverrideEntryOnlyNewer { get; private set; }
        /// <summary>
        /// If true, the entire group of the target database will be overwritten (only works with <see cref="OverrideTargetDatabase"/> == false).
        /// </summary>
        public bool OverrideEntireGroup { get; private set; }
        /// <summary>
        /// If true, this export job will be ignored.
        /// </summary>
        public bool Disabled { get; private set; }
        /// <summary>
        /// If true, only Username and Password will be exported to the target database.
        /// </summary>
        public bool ExportUserAndPassOnly { get; private set; }

        // Private constructor
        private Settings()
        {
        }

        private static ulong GetUlongValue(string key, PwEntry settingsEntry)
        {
            ulong result = 0;
            string value = settingsEntry.Strings.ReadSafe(key);
            if (!string.IsNullOrEmpty(value) && !ulong.TryParse(value, out result))
            {
                MessageService.ShowWarning("SubsetExport: " + key + " is given but can not be parsed as ulog for: " +
                                           settingsEntry.Strings.ReadSafe("Title"));
            }

            return result;
        }

        private static uint GetUIntValue(string key, PwEntry settingsEntry)
        {
            uint result = 0;
            string value = settingsEntry.Strings.ReadSafe(key);
            if (!string.IsNullOrEmpty(value) && !uint.TryParse(value, out result))
            {
                MessageService.ShowWarning("SubsetExport: " + key + " is given but can not be parsed as uint for: " +
                                           settingsEntry.Strings.ReadSafe("Title"));
            }

            return result;
        }

        /// <summary>
        /// Read all job settings from an entry.
        /// </summary>
        /// <param name="settingsEntry">The entry to read the settings from.</param>
        /// <param name="sourceDb">A database to resolve refs in the password field.</param>
        /// <returns>A settings object containing all the settings for this job.</returns>
        public static Settings Parse(PwEntry settingsEntry, PwDatabase sourceDb=null)
        {
            return new Settings()
            {
                Password = FieldHelper.GetFieldWRef(settingsEntry, sourceDb, PwDefs.PasswordField),
                TargetFilePath = settingsEntry.Strings.ReadSafe("SubsetExport_TargetFilePath"),
                KeyFilePath = settingsEntry.Strings.ReadSafe("SubsetExport_KeyFilePath"),
                Tag = settingsEntry.Strings.ReadSafe("SubsetExport_Tag"),
                KeyTransformationRounds = GetUlongValue("SubsetExport_KeyTransformationRounds", settingsEntry),
                RootGroupName = settingsEntry.Strings.ReadSafe("SubsetExport_RootGroupName"),
                Group = settingsEntry.Strings.ReadSafe("SubsetExport_Group"),
                FlatExport = settingsEntry.Strings.ReadSafe("SubsetExport_FlatExport").ToLower().Trim() == "true",
                OverrideTargetDatabase = settingsEntry.Strings.ReadSafe("SubsetExport_OverrideTargetDatabase").ToLower().Trim() != "false",
                OverrideEntryOnlyNewer = settingsEntry.Strings.ReadSafe("SubsetExport_OverrideEntryOnlyNewer").ToLower().Trim() == "true",
                OverrideEntireGroup = settingsEntry.Strings.ReadSafe("SubsetExport_OverrideEntireGroup").ToLower().Trim() == "true",
                Argon2ParamIterations = GetUlongValue("SubsetExport_Argon2ParamIterations", settingsEntry),
                Argon2ParamMemory = GetUlongValue("SubsetExport_Argon2ParamMemory", settingsEntry),
                Argon2ParamParallelism = GetUIntValue("SubsetExport_Argon2ParamParallelism", settingsEntry),
                Disabled = (settingsEntry.Expires && DateTime.Now > settingsEntry.ExpiryTime),
                ExportUserAndPassOnly = settingsEntry.Strings.ReadSafe("SubsetExport_ExportUserAndPassOnly").ToLower().Trim() == "true"
            };
        }
    }
}
