using System.Collections.Generic;

namespace KeePassSubsetExport.Tests.DataContainer
{
    internal class TestGroupValues
    {
        public string Uuid { get; set; }
        public string Name { get; set; }
        public List<TestEntryValues> Entries { get; set; }
        public List<TestGroupValues> SubGroups { get; set; }
    }
}
