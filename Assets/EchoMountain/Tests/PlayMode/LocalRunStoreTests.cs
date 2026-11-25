using System;
using System.IO;
using EchoMountain.Riding;
using NUnit.Framework;
using UnityEngine;

namespace EchoMountain.Tests
{
    public sealed class LocalRunStoreTests
    {
        private string directory;
        private string path;

        [SetUp]
        public void CreateTemporaryStore()
        {
            directory = Path.Combine(Path.GetTempPath(), "EchoMountain-Records-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            path = Path.Combine(directory, "runs.json");
        }

        [TearDown]
        public void RemoveTemporaryStore() => Directory.Delete(directory, true);

        [Test]
        public void RestartPreservesPreferencesAndSeparateRouteBests()
        {
            var store = new LocalRunStore(path);
            Assert.That(store.SetPreferences(false, MountainLine.Summit), Is.True);
            Assert.That(store.Finish(MountainLine.Pine, false, 84f, 45f, 100, "test"), Is.True);
            Assert.That(store.Finish(MountainLine.Pine, false, 86f, 43f, 260, "test"), Is.True);
            Assert.That(store.Finish(MountainLine.Summit, true, 21f, 40f, 450, "test"), Is.True);
            var restart = new LocalRunStore(path);
            Assert.That(restart.Data.sound, Is.False);
            Assert.That(restart.Data.selectedLine, Is.EqualTo(MountainLine.Summit));
            var pine = restart.Record(MountainLine.Pine, false);
            Assert.That(pine.finishes, Is.EqualTo(2));
            Assert.That(pine.fastest.seconds, Is.EqualTo(84f));
            Assert.That(pine.highestScore.points, Is.EqualTo(260));
            Assert.That(pine.previous.seconds, Is.EqualTo(86f));
            Assert.That(restart.Record(MountainLine.Summit, false).finishes, Is.Zero);
            Assert.That(restart.Record(MountainLine.Summit, true).highestScore.points, Is.EqualTo(450));
            Assert.That(File.Exists(path + ".bak"), Is.True);
            Assert.That(File.Exists(path + ".tmp"), Is.False);
        }

        [Test]
        public void NewRulesSaveSeparatelyWithoutChangingEarlierRecords()
        {
            string oldPath = Path.Combine(directory, "runs-v1.json");
            string earlierRecords = "Existing rules and records stay here.";
            File.WriteAllText(oldPath, earlierRecords);
            File.WriteAllText(oldPath + ".bak", earlierRecords);
            string r2Path = Path.Combine(directory, "runs-glacier-pass-c1-r2.json");
            File.WriteAllText(r2Path, earlierRecords);
            File.WriteAllText(r2Path + ".bak", earlierRecords);
            string r3Path = Path.Combine(directory, "runs-glacier-pass-c1-r3.json");
            File.WriteAllText(r3Path, earlierRecords);
            string r4Path = Path.Combine(directory, "runs-glacier-pass-c1-r4.json");
            File.WriteAllText(r4Path, earlierRecords);
            string newPath = Path.Combine(directory, LocalRunStore.FileName);
            var store = new LocalRunStore(newPath);
            Assert.That(store.Finish(MountainLine.Pine, false, 85f, 43f, 130, "test"), Is.True);
            Assert.That(new LocalRunStore(newPath).Record(MountainLine.Pine, false).finishes, Is.EqualTo(1));
            Assert.That(File.ReadAllText(oldPath), Is.EqualTo(earlierRecords));
            Assert.That(File.ReadAllText(oldPath + ".bak"), Is.EqualTo(earlierRecords));
            Assert.That(File.ReadAllText(r2Path), Is.EqualTo(earlierRecords));
            Assert.That(File.ReadAllText(r2Path + ".bak"), Is.EqualTo(earlierRecords));
            Assert.That(File.ReadAllText(r3Path), Is.EqualTo(earlierRecords));
            Assert.That(File.ReadAllText(r4Path), Is.EqualTo(earlierRecords));
            Assert.That(LocalRunStore.RulesVersion, Is.EqualTo(5));
        }

        [Test]
        public void UpdatedCourseKeepsPreferencesAndPracticeFromPreviousCourse()
        {
            var earlier = new LocalRunStore(path);
            Assert.That(earlier.SetPreferences(false, MountainLine.Summit));
            Assert.That(earlier.Finish(MountainLine.Pine, false, 84f, 45f, 260, "test"));
            Assert.That(earlier.Finish(MountainLine.Summit, true, 20f, 40f, 300, "test"));
            earlier.Data.courseVersion = 1;
            string earlierJson = JsonUtility.ToJson(earlier.Data);
            string previous = Path.Combine(directory, "runs-glacier-pass-c1-r5.json");
            File.WriteAllText(previous, earlierJson);

            var updated = LocalRunStore.OpenDirectory(directory);
            Assert.That(updated.Data.courseVersion, Is.EqualTo(2));
            Assert.That(updated.Data.sound, Is.False);
            Assert.That(updated.Data.selectedLine, Is.EqualTo(MountainLine.Summit));
            Assert.That(updated.Record(MountainLine.Pine, false).finishes, Is.Zero);
            Assert.That(updated.Record(MountainLine.Summit, true).highestScore.points, Is.EqualTo(300));
            Assert.That(updated.SetPreferences(false, MountainLine.Pine));

            Assert.That(LocalRunStore.OpenDirectory(directory).Record(MountainLine.Summit, true).finishes, Is.EqualTo(1));
            Assert.That(File.ReadAllText(previous), Is.EqualTo(earlierJson));
        }

        [Test]
        public void CorruptPrimaryRecoversBackupAndPreservesItOnNextWrite()
        {
            var store = new LocalRunStore(path);
            store.Finish(MountainLine.Pine, false, 84f, 45f, 260, "test");
            store.SetPreferences(false, MountainLine.Summit);
            string backup = File.ReadAllText(path + ".bak");
            File.WriteAllText(path, "{ truncated");
            File.WriteAllText(path + ".tmp", "unfinished write");
            var recovered = new LocalRunStore(path);
            Assert.That(recovered.Notice, Does.Contain("Recovered"));
            Assert.That(recovered.Record(MountainLine.Pine, false).finishes, Is.EqualTo(1));
            Assert.That(recovered.SetPreferences(false, MountainLine.Pine), Is.True);
            Assert.That(File.ReadAllText(path + ".bak"), Is.EqualTo(backup));
            Assert.That(File.ReadAllText(path + ".invalid"), Is.EqualTo("{ truncated"));
            Assert.That(new LocalRunStore(path).Data.sound, Is.False);
        }

        [TestCase("{}")]
        [TestCase("{\"schema\":1}")]
        public void InvalidOrIncompleteJsonStartsFresh(string damaged)
        {
            File.WriteAllText(path, damaged);
            var store = new LocalRunStore(path);
            Assert.That(store.Record(MountainLine.Pine, false).finishes, Is.Zero);
            // A schema with missing metadata is protected as incompatible; {} is corrupt.
            if (damaged == "{}") Assert.That(store.SetPreferences(false, MountainLine.Pine), Is.True);
            else Assert.That(store.SetPreferences(false, MountainLine.Pine), Is.False);
        }

        [Test]
        public void FutureSchemaAndDifferentRulesAreNeverOverwritten()
        {
            var store = new LocalRunStore(path);
            store.SetPreferences(true, MountainLine.Pine);
            var data = JsonUtility.FromJson<LocalRunData>(File.ReadAllText(path));
            data.schema = 2;
            string future = JsonUtility.ToJson(data);
            File.WriteAllText(path, future);
            var restart = new LocalRunStore(path);
            Assert.That(restart.SetPreferences(false, MountainLine.Summit), Is.False);
            Assert.That(File.ReadAllText(path), Is.EqualTo(future));
            data.schema = 1;
            data.rulesVersion++;
            string otherRules = JsonUtility.ToJson(data);
            File.WriteAllText(path, otherRules);
            Assert.That(new LocalRunStore(path).SetPreferences(false, MountainLine.Pine), Is.False);
            Assert.That(File.ReadAllText(path), Is.EqualTo(otherRules));
        }

        [Test]
        public void InvalidNumbersAndFailedWritesDoNotCrashTheSession()
        {
            var store = new LocalRunStore(path);
            Assert.That(store.Finish(MountainLine.Pine, false, float.NaN, 45f, 1, "test"), Is.False);
            Assert.That(store.Finish(MountainLine.Pine, false, 84f, float.PositiveInfinity, 1, "test"), Is.False);
            Assert.That(store.Finish(MountainLine.Pine, false, 84f, 45f, -1, "test"), Is.False);
            Assert.That(store.Record(MountainLine.Pine, false).finishes, Is.Zero);
            File.WriteAllText(path, "parent is a file");
            var blocked = new LocalRunStore(Path.Combine(path, "child.json"));
            Assert.That(blocked.SetPreferences(false, MountainLine.Pine), Is.False);
            Assert.That(blocked.Data.sound, Is.False);
            Assert.That(blocked.Notice, Does.Contain("Could not save"));
        }
    }
}
