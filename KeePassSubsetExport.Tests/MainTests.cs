using System;
using System.IO;
using System.Linq;
using KeePassLib;
using KeePassLib.Cryptography.KeyDerivation;
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

            // Check if test is running on AzureDevops
            string azureDevOpsSourcePath = Environment.GetEnvironmentVariable("System.DefaultWorkingDirectory");
            Console.WriteLine("###System.DefaultWorkingDirectory: " + azureDevOpsSourcePath);
            if (!string.IsNullOrEmpty(azureDevOpsSourcePath) && Directory.Exists(azureDevOpsSourcePath))
            {
                // Running on AzureDevOps
                _settings.RootPath = azureDevOpsSourcePath;
            }
            else
            {
                // Local test run
                _settings.RootPath =
                Path.GetDirectoryName(Path.GetDirectoryName(testContext.TestDir)) ?? throw new InvalidOperationException();
            }            

            _settings.DbAFilesPath = Path.Combine(_settings.RootPath, @"TestDatabases\A\");
            _settings.DbBFilesPath = Path.Combine(_settings.RootPath, @"TestDatabases\B\");
            _settings.DbCFilesPath = Path.Combine(_settings.RootPath, @"TestDatabases\C\");

            _settings.DbMainPw = "Test";
            _settings.DbAPath = Path.Combine(_settings.DbAFilesPath, "A.kdbx");
            _settings.DbBPath = Path.Combine(_settings.DbBFilesPath, "B.kdbx");
            _settings.DbCPath = Path.Combine(_settings.DbCFilesPath, "C.kdbx");

            _settings.DbTestPw = "TargetPw";
            _settings.KeyTestAPath = Path.Combine(_settings.DbAFilesPath, "A.key");
            _settings.KeyTestBPath = Path.Combine(_settings.DbBFilesPath, "B.key");

            // Delete old files
            foreach (var filePathToDelete in Directory.GetFiles(_settings.DbAFilesPath, "*_*.kdbx"))
            {
                File.Delete(filePathToDelete);
            }
        }

        [ClassInitialize()]
        public static void Initalize(TestContext testContext)
        {
            InitalizeSettings(testContext);

            PwDatabase db = DbHelper.OpenDatabase(_settings.DbAPath, _settings.DbMainPw);

            Exporter.Export(db);

            db.Close();

            db = DbHelper.OpenDatabase(_settings.DbBPath, _settings.DbMainPw);

            Exporter.Export(db);

            db.Close();

            db = DbHelper.OpenDatabase(_settings.DbCPath, _settings.DbMainPw);

            Exporter.Export(db);

            db.Close();
        }

        [TestMethod]
        public void Ae1Test()
        {
            PwDatabase db = DbHelper.OpenDatabase(Path.Combine(_settings.DbAFilesPath, Ae1RealData.Db), password:_settings.DbTestPw,
                keyPath:_settings.KeyTestAPath);

            var group = db.RootGroup;

            CheckKdf(db.KdfParameters, Ae1RealData.Kdf);

            CheckGroup(group, Ae1RealData.Data);

            db.Close();
        }

        [TestMethod]
        public void Ae2Test()
        {
            PwDatabase db = DbHelper.OpenDatabase(Path.Combine(_settings.DbAFilesPath, Ae2RealData.Db), keyPath: _settings.KeyTestAPath);

            var group = db.RootGroup;

            CheckKdf(db.KdfParameters, Ae2RealData.Kdf);

            CheckGroup(group, Ae2RealData.Data);

            db.Close();
        }

        [TestMethod]
        public void Ae3Test()
        {
            PwDatabase db = DbHelper.OpenDatabase(Path.Combine(_settings.DbAFilesPath, Ae3RealData.Db), password: _settings.DbTestPw);

            var group = db.RootGroup;

            CheckKdf(db.KdfParameters, Ae3RealData.Kdf);

            CheckGroup(group, Ae3RealData.Data);

            db.Close();
        }

        [TestMethod]
        public void Ae4Test()
        {
            PwDatabase db = DbHelper.OpenDatabase(Path.Combine(_settings.DbAFilesPath, Ae4RealData.Db), password: _settings.DbTestPw);

            var group = db.RootGroup;

            CheckKdf(db.KdfParameters, Ae4RealData.Kdf);

            CheckGroup(group, Ae4RealData.Data);

            db.Close();
        }

        [TestMethod]
        public void Ae5Test()
        {
            PwDatabase db = DbHelper.OpenDatabase(Path.Combine(_settings.DbAFilesPath, Ae5RealData.Db), password: _settings.DbTestPw);

            var group = db.RootGroup;

            CheckKdf(db.KdfParameters, Ae5RealData.Kdf);

            CheckGroup(group, Ae5RealData.Data);

            db.Close();
        }

        [TestMethod]
        public void Ae6Test()
        {
            PwDatabase db = DbHelper.OpenDatabase(Path.Combine(_settings.DbAFilesPath, Ae6RealData.Db), password: _settings.DbTestPw);

            var group = db.RootGroup;

            CheckKdf(db.KdfParameters, Ae6RealData.Kdf);

            CheckGroup(group, Ae6RealData.Data);

            db.Close();
        }

        [TestMethod]
        public void Ae7Test()
        {
            Assert.IsFalse(File.Exists(Path.Combine(_settings.DbCFilesPath, Ae7RealData.Db)), "An disabled/expired job was executed.");
        }

        [TestMethod]
        public void Be1Test()
        {
            PwDatabase db = DbHelper.OpenDatabase(Path.Combine(_settings.DbBFilesPath, Be1RealData.Db), keyPath: _settings.KeyTestBPath);

            var group = db.RootGroup;

            CheckKdf(db.KdfParameters, Be1RealData.Kdf);

            CheckGroup(group, Be1RealData.Data);

            db.Close();
        }

        [TestMethod]
        public void Be2Test()
        {
            PwDatabase db = DbHelper.OpenDatabase(Path.Combine(_settings.DbBFilesPath, Be2RealData.Db), keyPath: _settings.KeyTestAPath);

            var group = db.RootGroup;

            CheckKdf(db.KdfParameters, Be2RealData.Kdf);

            CheckGroup(group, Be2RealData.Data);

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

        private static void CheckKdf(KdfParameters param, TestKdfValues testEntryValues)
        {
            Assert.AreEqual(testEntryValues.KdfUuid, param.KdfUuid);
            if (param.KdfUuid.Equals(TestKdfValues.UuidAes))
            {
                Assert.AreEqual(testEntryValues.AesKeyTransformationRounds, param.GetUInt64(AesKdf.ParamRounds, 0));
            }
            else if(param.KdfUuid.Equals(TestKdfValues.UuidArgon2))
            {
                Assert.AreEqual(testEntryValues.Argon2Iterations, param.GetUInt64(Argon2Kdf.ParamIterations, 0));
                Assert.AreEqual(testEntryValues.Argon2Memory, param.GetUInt64(Argon2Kdf.ParamMemory, 0));
                Assert.AreEqual(testEntryValues.Argon2Parallelism, param.GetUInt32(Argon2Kdf.ParamParallelism, 0));
            }
            else
            {
                Assert.Fail("Kdf is not Aes or Argon2"); 
            }
        }

        #endregion

    }
}
