using System;
using System.IO;
using System.Linq;
using KeePassLib;
using KeePassSubsetExport.Tests.ComparisonData;
using KeePassSubsetExport.Tests.DataContainer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KeePassSubsetExport.Tests
{
    [TestClass]
    public class MainTests
    {
        private static TestSettings _settings;


        private static void InitalizeSettings(TestContext testContext)
        {
            _settings = new TestSettings();

            _settings.RootPath =
                Path.GetDirectoryName(Path.GetDirectoryName(testContext.TestDir)) ??throw new InvalidOperationException();

            _settings.DbAFilesPath = Path.Combine(_settings.RootPath, @"TestDatabases\A\");

            _settings.DbMainPw = "Test";
            _settings.DbMainPath = Path.Combine(_settings.DbAFilesPath, "A.kdbx");

            _settings.DbTestPw = "TargetPw";
            _settings.KeyTestPath = Path.Combine(_settings.DbAFilesPath, "A.key");

            // Delete old files
            foreach (var filePathToDelete in Directory.GetFiles(_settings.RootPath, "*_*.kdbx"))
            {
                File.Delete(filePathToDelete);
            }
        }

        [ClassInitialize()]
        public static void Initalize(TestContext testContext)
        {
            InitalizeSettings(testContext);

            PwDatabase db = DbHelper.OpenDatabase(_settings.DbMainPath, _settings.DbMainPw);

            Exporter.Export(db);

            db.Close();
        }

        [TestMethod]
        public void Ae1Test()
        {
            PwDatabase db = DbHelper.OpenDatabase(Path.Combine(_settings.DbAFilesPath, Ae1RealData.Db), password:_settings.DbTestPw,
                keyPath:_settings.KeyTestPath);

            var group = db.RootGroup;

            CheckGroup(group, Ae1RealData.Data);

            db.Close();
        }

        [TestMethod]
        public void Ae2Test()
        {
            PwDatabase db = DbHelper.OpenDatabase(Path.Combine(_settings.DbAFilesPath, Ae2RealData.Db), keyPath: _settings.KeyTestPath);

            var group = db.RootGroup;

            CheckGroup(group, Ae2RealData.Data);

            db.Close();
        }

        [TestMethod]
        public void Ae3Test()
        {
            PwDatabase db = DbHelper.OpenDatabase(Path.Combine(_settings.DbAFilesPath, Ae3RealData.Db), password: _settings.DbTestPw);

            var group = db.RootGroup;

            CheckGroup(group, Ae3RealData.Data);

            db.Close();
        }

        [TestMethod]
        public void Ae4Test()
        {
            PwDatabase db = DbHelper.OpenDatabase(Path.Combine(_settings.DbAFilesPath, Ae3RealData.Db), password: _settings.DbTestPw);

            var group = db.RootGroup;

            CheckGroup(group, Ae3RealData.Data);

            db.Close();
        }

        #region Content test functions

        private static void CheckGroup(PwGroup group, TestGroupValues data)
        {
            Assert.IsNotNull(group);
            Assert.AreEqual(data.Uuid, group.Uuid.ToHexString());
            Assert.AreEqual(data.Name, group.Name);
            CollectionAssert.AreEquivalent(data.SubGroups?.Select(x => x.Uuid).ToArray() ?? (new string[0]), group.Groups?.Select(x => x.Uuid.ToHexString()).ToArray() ??(new string[0]));
            CollectionAssert.AreEquivalent(data.Entries?.Select(x => x.Uuid).ToArray() ?? (new string[0]), group.Entries?.Select(x => x.Uuid.ToHexString()).ToArray() ?? (new string[0]));

            if (group.Entries != null)
            {
                foreach (PwEntry entry in group.Entries)
                {
                    CheckEntry(entry, (data.Entries ?? throw new InvalidOperationException()).First(x => x.Uuid == entry.Uuid.ToHexString()));
                }
            }

            if (group.Groups != null)
            {
                foreach (PwGroup subGroup in group.Groups)
                {
                    CheckGroup(subGroup, (data.SubGroups ?? throw new InvalidOperationException()).First(x => x.Uuid == subGroup.Uuid.ToHexString()));
                }
            }
        }

        private static void CheckEntry(PwEntry entry, TestEntryValues testEntryValues)
        {
            Assert.IsNotNull(entry);
            Assert.AreEqual(testEntryValues.Title, entry.Strings.ReadSafe("Title"));
            Assert.AreEqual(testEntryValues.UserName, entry.Strings.ReadSafe("UserName"));
            Assert.AreEqual(testEntryValues.Password, entry.Strings.ReadSafe("Password"));
            Assert.AreEqual(testEntryValues.Url, entry.Strings.ReadSafe("URL"));
        }

        #endregion

    }
}
