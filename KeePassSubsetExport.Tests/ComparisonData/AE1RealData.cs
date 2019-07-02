using System.Collections.Generic;
using KeePassSubsetExport.Tests.DataContainer;

namespace KeePassSubsetExport.Tests.ComparisonData
{
    internal static class Ae1RealData
    {
        public static string Db => "A_E1.kdbx";

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
                Entries = new List<TestEntryValues>()
                {
                    new TestEntryValues()
                    {
                        Uuid = "50A6AAE7ABF82F4C9FA8533143988C60",
                        Title = "A_E2",
                        UserName = "user",
                        Password = "dummy",
                        Url = "https://github.com/lukeIam/KeePassSubsetExport"
                    },
                    new TestEntryValues()
                    {
                        Uuid = "1569F3F77FA4C44F9D8FC9DEEF141629",
                        Title = "A_E3",
                        UserName = "user",
                        Password = "dummy",
                        Url = "https://github.com/lukeIam/KeePassSubsetExport"
                    }
                },
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
                                Uuid = "743328509472674E821FB1FB778BC2BD",
                                Title = "A_G1_E2",
                                UserName = "user",
                                Password = "dummy",
                                Url = "https://github.com/lukeIam/KeePassSubsetExport"
                            },
                            new TestEntryValues()
                            {
                                Uuid = "A3226A9877AD924F87182B66530B1BDF",
                                Title = "A_G1_E3",
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
                                        Uuid = "7A9C95AC44FE2041B663C46549C21369",
                                        Title = "A_G2_E2",
                                        UserName = "user",
                                        Password = "dummy",
                                        Url = "https://github.com/lukeIam/KeePassSubsetExport"
                                    },
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
