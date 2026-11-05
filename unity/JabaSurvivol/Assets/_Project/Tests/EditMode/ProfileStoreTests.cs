using System;
using System.IO;
using JabaSurvivol.Core;
using JabaSurvivol.Gameplay;
using NUnit.Framework;

namespace JabaSurvivol.Tests.EditMode
{
    public sealed class ProfileStoreTests
    {
        private string _directory;
        private string _path;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "jabasurvivol-tests", Guid.NewGuid().ToString("N"));
            _path = Path.Combine(_directory, ProfileStore.FileName);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }

        [Test]
        public void Save_and_load_round_trips_persistent_progress()
        {
            var store = new ProfileStore(_path);
            var profile = new Profile { Supplies = 730, Level = 8, Xp = 55, SkillPoints = 9 };
            profile.AddSupplies(70);

            store.Save(profile);
            var loaded = store.Load();

            Assert.That(File.Exists(_path), Is.True);
            Assert.That(loaded.Supplies, Is.EqualTo(800));
            Assert.That(loaded.Level, Is.EqualTo(8));
            Assert.That(loaded.Xp, Is.EqualTo(55));
            Assert.That(loaded.SkillPoints, Is.EqualTo(9));
            Assert.That(loaded.SchemaVersion, Is.EqualTo(Profile.CurrentSchemaVersion));
        }

        [Test]
        public void Load_returns_defaults_when_file_is_missing()
        {
            var store = new ProfileStore(_path);

            var loaded = store.Load();

            Assert.That(loaded.Supplies, Is.EqualTo(0));
            Assert.That(loaded.Level, Is.EqualTo(1));
        }

        [Test]
        public void Load_recovers_from_corrupt_file_and_keeps_a_backup()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(_path, "{ this is not valid json");
            var store = new ProfileStore(_path);

            var loaded = store.Load();

            Assert.That(loaded.Level, Is.EqualTo(1));
            Assert.That(File.Exists(_path + ".corrupt"), Is.True);
        }
    }
}
