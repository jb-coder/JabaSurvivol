using System;
using System.Collections;
using System.IO;
using JabaSurvivol.Core;
using JabaSurvivol.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JabaSurvivol.Tests.PlayMode
{
    public sealed class SynergyPlayModeTests
    {
        private string _directory;
        private string _path;
        private GameBootstrap _bootstrap;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "jabasurvivol-synergy", Guid.NewGuid().ToString("N"));
            _path = Path.Combine(_directory, ProfileStore.FileName);
            GameBootstrap.SavePathOverride = _path;
        }

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.SavePathOverride = null;

            if (_bootstrap != null)
            {
                UnityEngine.Object.DestroyImmediate(_bootstrap.gameObject);
                _bootstrap = null;
            }

            if (Directory.Exists(_directory))
            {
                try
                {
                    Directory.Delete(_directory, true);
                }
                catch (IOException)
                {
                }
            }
        }

        [UnityTest]
        public IEnumerator Berserker_increases_damage_below_half_health()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.Profile.Level = 30;
            session.Profile.SetSkillRank(SkillNode.Strength, 3);
            session.Profile.SetSkillRank(SkillNode.Vitality, 3);
            session.StartRun();
            yield return null;
            var baseline = session.Stats.EffectiveWeaponDamage;

            session.ApplyDamage(session.Run.Player.MaxHealth * 0.6f);
            yield return null;

            Assert.That(session.Run.Player.Health, Is.LessThan(session.Run.Player.MaxHealth * 0.5f));
            Assert.That(session.Stats.EffectiveWeaponDamage, Is.GreaterThan(baseline));
        }

        [UnityTest]
        public IEnumerator Ghost_grants_move_speed_for_two_seconds_after_a_kill()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.Profile.Level = 30;
            session.Profile.SetSkillRank(SkillNode.Speed, 3);
            session.Profile.SetSkillRank(SkillNode.Precision, 3);
            session.StartRun();
            yield return null;
            var baseline = session.Stats.MoveSpeed;

            session.DamageZombieForTests(100000f);
            yield return null;

            Assert.That(session.Stats.MoveSpeed, Is.GreaterThan(baseline));

            yield return new WaitForSeconds(2.3f);
            Assert.That(session.Stats.MoveSpeed, Is.EqualTo(baseline).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator Bastion_regenerates_armor_out_of_combat()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.Profile.Level = 30;
            session.Profile.SetSkillRank(SkillNode.Endurance, 3);
            session.Profile.SetSkillRank(SkillNode.Engineering, 3);
            session.Profile.UnlockArmor(ArmorTier.Light);
            session.Profile.EquipArmor(ArmorTier.Light);
            session.StartRun();
            yield return null;
            Assert.That(session.Profile.GetSkillRank(SkillNode.Endurance), Is.EqualTo(3));
            Assert.That(session.Profile.GetSkillRank(SkillNode.Engineering), Is.EqualTo(3));
            Assert.That(session.Run.Synergy.BastionActive(session.Profile), Is.True);
            session.EnemySpawningEnabledForTests = false;
            session.DamageZombieForTests(100000f);
            yield return null;

            session.ApplyDamage(10f);
            var armorAfterHit = session.Run.Armor;
            Assert.That(armorAfterHit, Is.LessThan(session.Run.MaxArmor));

            var regenerated = session.Run.TickSynergies(5.1f);

            Assert.That(regenerated, Is.GreaterThan(0));
            Assert.That(session.Run.Armor, Is.GreaterThan(armorAfterHit));
        }

        [UnityTest]
        public IEnumerator Berserker_survives_a_weapon_switch()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.Profile.Level = 30;
            session.Profile.UnlockWeapon(WeaponId.Smg);
            session.Profile.EquipWeapon(WeaponId.Smg);
            session.Profile.SetSkillRank(SkillNode.Strength, 3);
            session.Profile.SetSkillRank(SkillNode.Vitality, 3);
            session.StartRun();
            yield return null;

            session.ApplyDamage(session.Run.Player.MaxHealth * 0.6f);
            yield return null;
            var multiplier = session.Stats.WeaponDamageMultiplier;

            Assert.That(session.SwitchWeapon(), Is.True);
            yield return null;

            Assert.That(
                session.Stats.WeaponDamageMultiplier,
                Is.EqualTo(multiplier).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator Character_sheet_shows_synergy_status()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.Profile.Level = 30;
            session.Profile.SetSkillRank(SkillNode.Strength, 3);
            session.Profile.SetSkillRank(SkillNode.Vitality, 3);
            session.ShowCharacterSheet();
            yield return null;

            Assert.That(session.CharacterSheetVisible, Is.True);
            Assert.That(session.SynergyTextVisible, Is.True);
            Assert.That(session.SynergyTextForTests, Does.Contain("BERSERKER ACTIVE"));
            Assert.That(session.SynergyTextForTests, Does.Contain("SPEED"));
            Assert.That(session.SynergyTextForTests, Does.Contain("PRECISION"));
        }

        private static GameBootstrap CreateBootstrap()
        {
            var gameObject = new GameObject("Test Bootstrap");
            return gameObject.AddComponent<GameBootstrap>();
        }
    }
}
