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
            var profile = new Profile { Supplies = 730, Level = 8, Xp = 55, SkillPoints = 8 };
            profile.AddSupplies(70);

            store.Save(profile);
            var loaded = store.Load();

            Assert.That(File.Exists(_path), Is.True);
            Assert.That(loaded.Supplies, Is.EqualTo(800));
            Assert.That(loaded.Level, Is.EqualTo(8));
            Assert.That(loaded.Xp, Is.EqualTo(55));
            Assert.That(loaded.SkillPoints, Is.EqualTo(8));
            Assert.That(loaded.SchemaVersion, Is.EqualTo(Profile.CurrentSchemaVersion));
        }

        [Test]
        public void Save_and_load_round_trips_the_highest_round()
        {
            var store = new ProfileStore(_path);
            var profile = new Profile { HighestRound = 7 };

            store.Save(profile);
            var loaded = store.Load();

            Assert.That(loaded.HighestRound, Is.EqualTo(7));
        }

        [Test]
        public void Save_and_load_round_trips_weapon_unlocks_and_equipped_weapon()
        {
            var store = new ProfileStore(_path);
            var profile = new Profile();
            profile.AddSupplies(700);

            Assert.That(ArmoryService.PurchaseWeapon(profile, WeaponId.Smg), Is.EqualTo(ArmoryPurchaseResult.Purchased));
            Assert.That(ArmoryService.EquipWeapon(profile, WeaponId.Smg), Is.True);

            store.Save(profile);
            var loaded = store.Load();

            Assert.That(loaded.HasWeapon(WeaponId.Smg), Is.True);
            Assert.That(loaded.EquippedWeapon, Is.EqualTo(WeaponId.Smg));
            Assert.That(loaded.Supplies, Is.EqualTo(200));
        }

        [Test]
        public void Save_and_load_round_trips_weapon_upgrades()
        {
            var store = new ProfileStore(_path);
            var profile = new Profile();
            profile.AddSupplies(1000);
            WeaponUpgradeService.TryPurchase(profile, WeaponId.Pistol, WeaponUpgradeStat.Damage);
            WeaponUpgradeService.TryPurchase(profile, WeaponId.Pistol, WeaponUpgradeStat.Damage);

            store.Save(profile);
            var loaded = store.Load();

            Assert.That(loaded.GetWeaponUpgradeTier(WeaponId.Pistol, WeaponUpgradeStat.Damage), Is.EqualTo(2));
            Assert.That(loaded.Supplies, Is.EqualTo(850));
        }

        [Test]
        public void Load_adds_the_pistol_to_a_legacy_save_without_weapon_fields()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(
                _path,
                "{\"SchemaVersion\":1,\"Supplies\":123,\"Level\":4,\"Xp\":10,\"SkillPoints\":3}");
            var store = new ProfileStore(_path);

            var loaded = store.Load();

            Assert.That(loaded.Supplies, Is.EqualTo(123));
            Assert.That(loaded.HasWeapon(WeaponId.Pistol), Is.True);
            Assert.That(loaded.EquippedWeapon, Is.EqualTo(WeaponId.Pistol));
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
        public void Save_and_load_round_trips_armor_unlocks_equip_and_upgrades()
        {
            var store = new ProfileStore(_path);
            var profile = new Profile();
            profile.AddSupplies(1000);
            ArmorService.PurchaseArmor(profile, ArmorTier.Medium);
            ArmorService.EquipArmor(profile, ArmorTier.Medium);
            ArmorService.TryPurchaseUpgrade(profile, ArmorTier.Medium);

            store.Save(profile);
            var loaded = store.Load();

            Assert.That(loaded.HasArmor(ArmorTier.Medium), Is.True);
            Assert.That(loaded.EquippedArmor, Is.EqualTo(ArmorTier.Medium));
            Assert.That(loaded.GetArmorUpgradeTier(ArmorTier.Medium), Is.EqualTo(1));
            Assert.That(loaded.Supplies, Is.EqualTo(0));
        }

        [Test]
        public void Save_and_load_round_trips_skill_allocation_and_active_skill()
        {
            var store = new ProfileStore(_path);
            var profile = new Profile { Level = 10, SkillPoints = 11 };
            Assert.That(profile.AllocateSkill(SkillNode.Strength), Is.True);
            Assert.That(profile.AllocateSkill(SkillNode.Vitality), Is.True);

            store.Save(profile);
            var loaded = store.Load();

            Assert.That(loaded.GetSkillRank(SkillNode.Strength), Is.EqualTo(1));
            Assert.That(loaded.GetSkillRank(SkillNode.Vitality), Is.EqualTo(1));
            Assert.That(loaded.SkillPoints, Is.EqualTo(9));
            Assert.That(loaded.ActiveSkill, Is.EqualTo(SkillNode.Turret));
        }

        [Test]
        public void Load_adds_none_armor_to_a_legacy_save_without_armor_fields()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(
                _path,
                "{\"SchemaVersion\":1,\"Supplies\":123,\"Level\":4,\"Xp\":10,\"SkillPoints\":3}");
            var store = new ProfileStore(_path);

            var loaded = store.Load();

            Assert.That(loaded.HasArmor(ArmorTier.None), Is.True);
            Assert.That(loaded.EquippedArmor, Is.EqualTo(ArmorTier.None));
            Assert.That(loaded.GetArmorUpgradeTier(ArmorTier.None), Is.EqualTo(0));
            Assert.That(loaded.GetSkillRank(SkillNode.Strength), Is.EqualTo(0));
            Assert.That(loaded.ActiveSkill, Is.EqualTo(SkillNode.Turret));
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

        [Test]
        public void Load_recovers_from_backup_when_the_main_file_is_corrupt()
        {
            var store = new ProfileStore(_path);
            store.Save(new Profile { Supplies = 111 });
            store.Save(new Profile { Supplies = 222 });
            File.WriteAllText(_path, "{ broken");

            var loaded = store.Load();

            Assert.That(loaded.Supplies, Is.EqualTo(111));
            Assert.That(File.Exists(_path), Is.True);
            Assert.That(File.ReadAllText(_path), Does.Contain("\"Supplies\": 111"));
            Assert.That(File.Exists(_path + ".corrupt"), Is.True);
        }

        [Test]
        public void Load_recovers_from_backup_when_the_main_file_is_missing()
        {
            var store = new ProfileStore(_path);
            store.Save(new Profile { Supplies = 111 });
            store.Save(new Profile { Supplies = 222 });
            File.Delete(_path);

            var loaded = store.Load();

            Assert.That(loaded.Supplies, Is.EqualTo(111));
            Assert.That(File.Exists(_path), Is.True);
            Assert.That(File.ReadAllText(_path), Does.Contain("\"Supplies\": 111"));
        }

        [Test]
        public void Load_recovers_from_backup_when_the_main_file_is_empty()
        {
            var store = new ProfileStore(_path);
            store.Save(new Profile { Supplies = 111 });
            store.Save(new Profile { Supplies = 222 });
            File.WriteAllText(_path, string.Empty);

            var loaded = store.Load();

            Assert.That(loaded.Supplies, Is.EqualTo(111));
            Assert.That(File.ReadAllText(_path), Does.Contain("\"Supplies\": 111"));
            Assert.That(File.Exists(_path + ".corrupt"), Is.True);
        }

        [Test]
        public void Load_recovers_from_backup_when_the_main_file_parses_to_null()
        {
            var store = new ProfileStore(_path);
            store.Save(new Profile { Supplies = 111 });
            store.Save(new Profile { Supplies = 222 });
            File.WriteAllText(_path, "null");

            var loaded = store.Load();

            Assert.That(loaded.Supplies, Is.EqualTo(111));
            Assert.That(File.ReadAllText(_path), Does.Contain("\"Supplies\": 111"));
            Assert.That(File.Exists(_path + ".corrupt"), Is.True);
        }
    }
}
