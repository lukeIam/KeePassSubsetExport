using System;
using System.IO;
using KeePass;
using KeePass.Resources;
using KeePassLib.Keys;
using KeePassLib.Serialization;
using KeePassLib.Utility;
using KeePassLib;

namespace KeePassSubsetExport
{
    public static class KeyHelper
    {
        /// <summary>
        /// Adds a password to a <see cref="CompositeKey"/>.
        /// </summary>
        /// <param name="passwordByteArray">The password as byte array to add to the <see cref="CompositeKey"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/> to add the password to.</param>
        /// <returns>true if sucessfull, false otherwise.</returns>
        public static bool AddPasswordToKey(byte[] passwordByteArray, CompositeKey key)
        {
            if (passwordByteArray.Length == 0)
            {
                return false;
            }

            key.AddUserKey(new KcpPassword(passwordByteArray));
            return true;
        }

        /// <summary>
        /// Adds a password to a <see cref="CompositeKey"/>.
        /// </summary>
        /// <param name="password">The password to add to the <see cref="CompositeKey"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/> to add the password to.</param>
        /// <returns>true if sucessfull, false otherwise.</returns>
        public static bool AddPasswordToKey(string password, CompositeKey key)
        {
            if (password == "")
            {
                return false;
            }

            key.AddUserKey(new KcpPassword(password));
            return true;
        }

        /// <summary>
        /// Adds a keyfile to a <see cref="CompositeKey"/>.
        /// </summary>
        /// <param name="keyFilePath">The path to the keyfile to add to the <see cref="CompositeKey"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/> to add the keyfile to.</param>
        /// <param name="connectionInfo">The <see cref="IOConnectionInfo"/> object of the database (required for <see cref="KeyProviderQueryContext"/>).</param>
        /// <returns>true if sucessfull, false otherwise.</returns>
        public static bool AddKeyfileToKey(string keyFilePath, CompositeKey key, IOConnectionInfo connectionInfo)
        {
            bool success = false;

            if (!File.Exists(keyFilePath))
            {
                return false;
            }

            bool bIsKeyProv = Program.KeyProviderPool.IsKeyProvider(keyFilePath);

            if (!bIsKeyProv)
            {
                try
                {
                    key.AddUserKey(new KcpKeyFile(keyFilePath, true));
                    success = true;
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
                KeyProviderQueryContext ctxKp = new KeyProviderQueryContext(connectionInfo, true, false);

                KeyProvider prov = Program.KeyProviderPool.Get(keyFilePath);
                bool bPerformHash = !prov.DirectKey;
                byte[] pbCustomKey = prov.GetKey(ctxKp);

                if ((pbCustomKey != null) && (pbCustomKey.Length > 0))
                {
                    try
                    {
                        key.AddUserKey(new KcpCustomKey(keyFilePath, pbCustomKey, bPerformHash));
                        success = true;
                    }
                    catch (Exception exCkp)
                    {
                        MessageService.ShowWarning(exCkp);
                    }

                    MemUtil.ZeroByteArray(pbCustomKey);
                }
            }

            return success;
        }
    }
}
