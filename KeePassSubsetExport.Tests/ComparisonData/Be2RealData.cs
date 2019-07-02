using System.Collections.Generic;
using KeePassSubsetExport.Tests.DataContainer;

namespace KeePassSubsetExport.Tests.ComparisonData
{
    internal static class Be2RealData
    {
        public static string Db => "B_E2.kdbx";

        public static TestKdfValues Kdf => new TestKdfValues()
        {
            KdfUuid = TestKdfValues.UuidArgon2,
            Argon2Iterations = 3,
            Argon2Memory = 1024*1024*3,
            Argon2Parallelism = 3
        };

        public static TestGroupValues Data => Ae2RealData.Data;

    }
}
