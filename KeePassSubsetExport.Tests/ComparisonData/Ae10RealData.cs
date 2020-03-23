using KeePassSubsetExport.Tests.DataContainer;

namespace KeePassSubsetExport.Tests.ComparisonData
{
    internal static class Ae10RealData
    {
        public static string Db => "A_E10.kdbx";

        public static TestKdfValues Kdf => Ae1RealData.Kdf;
        public static TestGroupValues Data => Ae1RealData.Data;
    }
}