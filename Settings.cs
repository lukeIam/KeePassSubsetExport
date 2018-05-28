using KeePassLib;
using KeePassLib.Security;

namespace KeePassSubsetExport
{
    /// <summary>
    /// Contais all settings for a job.
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
        /// The keyTransformationRounds setting for the target database.
        /// </summary>
        public string KeyTransformationRoundsString { get; private set; }
        /// <summary>
        /// The parsed KeyTransformationRounds.
        /// </summary>
        public ulong KeyTransformationRounds { get; set; }
        /// <summary>
        /// The new name for the root group (optional).
        /// </summary>
        public string RootGroupName { get; private set; }
        /// <summary>
        /// The name of the group to export (optional if <see cref="Tag"/> is set).
        /// </summary>
        public string Group { get; private set; }
        /// <summary>
        /// True, if the export progress should ignore groups/folders, false otherwise (optional, defaults to false).
        /// </summary>
        public bool FlatExport { get; private set; }

        // Private constructor
        private Settings()
        {
        }
        
        /// <summary>
        /// Read all job settings from an entry.
        /// </summary>
        /// <param name="settingsEntry">The entry to read the settings from.</param>
        /// <returns>A settings object containing all the settings for this job.</returns>
        public static Settings Parse(PwEntry settingsEntry)
        {
            return new Settings()
            {
                Password = settingsEntry.Strings.GetSafe("Password"),
                TargetFilePath = settingsEntry.Strings.ReadSafe("SubsetExport_TargetFilePath"),
                KeyFilePath = settingsEntry.Strings.ReadSafe("SubsetExport_KeyFilePath"),
                Tag = settingsEntry.Strings.ReadSafe("SubsetExport_Tag"),
                KeyTransformationRoundsString = settingsEntry.Strings.ReadSafe("SubsetExport_KeyTransformationRounds"),
                RootGroupName = settingsEntry.Strings.ReadSafe("SubsetExport_RootGroupName"),
                Group = settingsEntry.Strings.ReadSafe("SubsetExport_Group"),
                FlatExport = settingsEntry.Strings.ReadSafe("SubsetExport_FlatExport").ToLower().Trim() == "true"
            };
        }
    }
}
