using System.Collections.Generic;
using KeePassSubsetExport.Tests.DataContainer;

namespace KeePassSubsetExport.Tests.ComparisonData
{
    internal static class Ae9RealData
    {
        public static string Db => "A_E9.kdbx";

        public static TestKdfValues Kdf => new TestKdfValues()
        {
            KdfUuid = TestKdfValues.UuidAes,
            AesKeyTransformationRounds = 60000
        };

        public static TestGroupValues Data =>
             new TestGroupValues()
             {
                 Uuid = "713CC7FA6D348E44AB570CD7CAB45DE0",
                 Name = "A",
                 SubGroups = new List<TestGroupValues>()
                 {
                    new TestGroupValues()
                    {
                        Uuid = "875668C19091E94CAA50D846132EFAD8",
                        Name = "A_G4",
                        Entries = new List<TestEntryValues>()
                        {
                            new TestEntryValues()
                            {
                                Uuid = "E2A3ED500A1E884886F4146E755D2129",
                                Title = "A_G3_E1",
                                UserName = "user",
                                Password = "dummy",
                                Url = "",
                                Note = ""
                            }
                        }
                    }
                 }
             };
    }
}
