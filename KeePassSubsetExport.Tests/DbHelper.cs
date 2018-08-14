using KeePassLib;
using KeePassLib.Interfaces;
using KeePassLib.Keys;
using KeePassLib.Serialization;

namespace KeePassSubsetExport.Tests
{
    internal class DbHelper
    {
        /// <summary>
        /// Opens a database.
        /// </summary>
        /// <param name="path">Path to the datebase.</param>
        /// <param name="password">Password of the database (optional).</param>
        /// <param name="keyPath">Keyfile for the database (optional).</param>
        /// <returns></returns>
        internal static PwDatabase OpenDatabase(string path, string password = null, string keyPath = null)
        {
            IOConnectionInfo ioConnInfo = new IOConnectionInfo { Path = path };
            CompositeKey compositeKey = new CompositeKey();

            if (password != null)
            {
                KeyHelper.AddPasswordToKey(password, compositeKey);
            }

            if (keyPath != null)
            {
                KeyHelper.AddKeyfileToKey(keyPath, compositeKey, ioConnInfo);
            }

            PwDatabase db = new PwDatabase();
            db.Open(ioConnInfo, compositeKey, new NullStatusLogger());
            return db;
        }
    }
}
