using System.Collections.Generic;
using KeePassSubsetExport.Tests.DataContainer;

namespace KeePassSubsetExport.Tests.ComparisonData
{
    internal static class Ae2RealData
    {
        public static string Db => "A_E2.kdbx";

        public static TestGroupValues Data =>
            new TestGroupValues()
            {
                Uuid = "713CC7FA6D348E44AB570CD7CAB45DE0",
                Name = "NewRoot",
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
                                Title = "A_G1_E1",
                                UserName = "user",
                                Password = "dummy",
                                Url = "https://github.com/lukeIam/KeePassSubsetExport"
                            },
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
                                        Uuid = "29C4322F054A804D9F37A7A67558C5BF",
                                        Title = "A_G2_E1",
                                        UserName = "user",
                                        Password = "dummy",
                                        Url = "https://github.com/lukeIam/KeePassSubsetExport"
                                    },
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
