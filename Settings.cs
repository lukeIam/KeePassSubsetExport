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
        /// The password to protect the target database(optional if <para>keyFilePath</para> is set)
        /// </summary>
        public ProtectedString Password { get; private set; }
        /// <summary>
        /// The path for the target database.
        /// </summary>
        public string TargetFilePath { get; private set; }
        /// <summary>
        /// The path to a key file to protect the target database (optional if <para>password</para> is set).
        /// </summary>
        public string KeyFilePath { get; private set; }
        /// <summary>
        /// Tag to export.
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
            };
        }
    }
}
