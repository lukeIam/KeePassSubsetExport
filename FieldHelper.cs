using System.Linq;
using KeePass.Util.Spr;
using KeePassLib;
using KeePassLib.Security;
using KeePassLib.Utility;

namespace KeePassSubsetExport
{
    public static class FieldHelper
    {
        private static readonly byte[] RefStringByteArray = new byte[]
        {
            123, 82, 69, 70, 58
        }; 

        public static ProtectedString GetFieldWRef(PwEntry entry, PwDatabase sourceDb, string fieldName)
        {
            ProtectedString orgValue = entry.Strings.GetSafe(fieldName);

            byte[] orgValueByteArray = orgValue.ReadUtf8();

            // Check if the protected string begins with the ref marker
            bool isRef = orgValueByteArray.Take(5).SequenceEqual(RefStringByteArray);

            MemUtil.ZeroByteArray(orgValueByteArray);

            if (!isRef)
            {
                // The protected string is not a ref -> return the protected string directly
                return orgValue;
            }

            
            SprContext ctx = new SprContext(entry, sourceDb,
                SprCompileFlags.All, false, false);

            // the protected string is a reference -> decode it and look it up
            return new ProtectedString(true, SprEngine.Compile(
                orgValue.ReadString(), ctx));
        }

        public static string GetFieldWRefUnprotected(PwEntry entry, PwDatabase sourceDb, string fieldName)
        {
            string orgValue = entry.Strings.ReadSafe(fieldName);            

            // Check if the string begins with the ref marker or contains a %
            if (!orgValue.StartsWith("{REF") && !orgValue.Contains("%"))
            {
                // The string is not a ref -> return the string directly
                return orgValue;
            }

            SprContext ctx = new SprContext(entry, sourceDb,
                SprCompileFlags.All, false, false);

            // the string is a reference -> decode it and look it up
            return SprEngine.Compile(orgValue, ctx);
        }
    }
}
