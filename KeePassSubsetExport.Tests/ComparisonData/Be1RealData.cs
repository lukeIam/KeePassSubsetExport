using System.Collections.Generic;
using KeePassSubsetExport.Tests.DataContainer;

namespace KeePassSubsetExport.Tests.ComparisonData
{
    internal static class Be1RealData
    {
        public static string Db => "B_E1.kdbx";

        public static TestKdfValues Kdf => new TestKdfValues()
        {
            KdfUuid = TestKdfValues.UuidArgon2,
            Argon2Iterations = 2,
            Argon2Memory = 1024*1024,
            Argon2Parallelism = 2
        };

        public static TestGroupValues Data => Ae2RealData.Data;

    }
}
