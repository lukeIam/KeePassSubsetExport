using System.Collections.Generic;
using KeePassSubsetExport.Tests.DataContainer;

namespace KeePassSubsetExport.Tests.ComparisonData
{
    internal static class Ae4RealData
    {
        public static string Db => "A_E4.kdbx";

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
                        Uuid = "3CABE6BBA6D7A048A2FBC37CA88C0819",
                        Name = "A_G1",
                        Entries = new List<TestEntryValues>()
                        {
                            new TestEntryValues()
                            {
                                Uuid = "017718804448F249ACFEF68C87D075C6",
                                Title = "A_G1_1",
                                UserName = "user",
                                Password = "dummy",
                                Url = "https://github.com/lukeIam/KeePassSubsetExport"
                            }
                        },
                        SubGroups = new List<TestGroupValues>()
                        {
                            new TestGroupValues()
                            {
                                Uuid = "C1FA77802458964681E18C642978A3C3",
                                Name = "A_G2",
                                Entries = new List<TestEntryValues>()
                                {
                                    new TestEntryValues()
                                    {
                                        Uuid = "DEB4B7620CC7B24096A87F45F8CEA919",
                                        Title = "A_G2_E3",
                                        UserName = "user",
                                        Password = "dummy",
                                        Url = "https://github.com/lukeIam/KeePassSubsetExport"
                                    }
                                }
                            }
                        }
                    }
                }
            };
    }
}
