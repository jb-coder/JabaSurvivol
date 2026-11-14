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
    public sealed class ArtUiIconPlayModeTests
    {
        private string _directory;
        private string _path;
        private GameBootstrap _bootstrap;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "jabasurvivol-ui-icons", Guid.NewGuid().ToString("N"));
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
        public IEnumerator Hud_exposes_weapon_and_active_skill_icons()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.Profile.SetSkillRank(SkillNode.Turret, 1);
            session.StartRun();
            yield return null;

            Assert.That(session.HudWeaponIconForTests, Is.Not.Null);
            Assert.That(session.HudWeaponIconForTests.name, Is.EqualTo("weapon_pistol"));
            Assert.That(session.ActiveSkillIconForTests, Is.Not.Null);
            Assert.That(session.ActiveSkillIconForTests.name, Is.EqualTo("skill_turret"));
        }

        [UnityTest]
        public IEnumerator Armory_exposes_every_weapon_icon()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.StartRun();
            yield return null;
            session.ShowArmory();
            yield return null;

            foreach (var weapon in WeaponCatalog.All)
            {
                var icon = session.ArmoryWeaponIconForTests(weapon.Id);
                Assert.That(icon, Is.Not.Null, weapon.Id.ToString());
            }
        }

        [UnityTest]
        public IEnumerator Reward_panel_exposes_every_reward_icon()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.StartRun();
            yield return null;

            session.ShowRoundEndForTests(new[]
            {
                new RoundRewardOption(RoundRewardType.Supplies, 100),
                new RoundRewardOption(RoundRewardType.Xp, 50),
                new RoundRewardOption(RoundRewardType.AmmoCrate, 1)
            });
            yield return null;

            Assert.That(session.RewardIconForTests(0), Is.Not.Null);
            Assert.That(session.RewardIconForTests(1), Is.Not.Null);
            Assert.That(session.RewardIconForTests(2), Is.Not.Null);
            Assert.That(session.RewardIconForTests(0).name, Is.EqualTo("reward_supplies"));
            Assert.That(session.RewardIconForTests(1).name, Is.EqualTo("reward_xp"));
            Assert.That(session.RewardIconForTests(2).name, Is.EqualTo("reward_ammo"));
        }

        [UnityTest]
        public IEnumerator Character_sheet_exposes_every_active_skill_icon()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.StartRun();
            yield return null;
            session.ShowCharacterSheet();
            yield return null;

            Assert.That(session.SelectActiveSkillIconForTests(SkillNode.Turret), Is.Not.Null);
            Assert.That(session.SelectActiveSkillIconForTests(SkillNode.Dash), Is.Not.Null);
            Assert.That(session.SelectActiveSkillIconForTests(SkillNode.Shockwave), Is.Not.Null);
        }

        private static GameBootstrap CreateBootstrap()
        {
            var gameObject = new GameObject("Test Bootstrap");
            return gameObject.AddComponent<GameBootstrap>();
        }
    }
}
