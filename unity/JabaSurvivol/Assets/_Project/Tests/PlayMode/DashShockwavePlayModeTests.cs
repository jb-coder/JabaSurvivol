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
    public sealed class DashShockwavePlayModeTests
    {
        private string _directory;
        private string _path;
        private GameBootstrap _bootstrap;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "jabasurvivol-dash", Guid.NewGuid().ToString("N"));
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
        public IEnumerator Dash_moves_the_player_and_grants_invulnerability()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.Profile.SetSkillRank(SkillNode.Dash, 1);
            session.Profile.SetActiveSkill(SkillNode.Dash);
            session.StartRun();
            yield return null;

            Assert.That(session.ActivateActiveSkillForTests(), Is.True);
            yield return null;

            Assert.That(session.PlayerPosition.x, Is.EqualTo(3f).Within(0.1f));

            var health = session.Run.Player.Health;
            session.ApplyDamage(10f);
            Assert.That(session.Run.Player.Health, Is.EqualTo(health));

            yield return new WaitForSeconds(GameBalance.DashInvulnerabilitySeconds + 0.2f);
            session.ApplyDamage(10f);
            Assert.That(session.Run.Player.Health, Is.LessThan(health));
        }

        [UnityTest]
        public IEnumerator Shockwave_damages_and_knocks_back_a_nearby_zombie()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.Profile.SetSkillRank(SkillNode.Shockwave, 1);
            session.Profile.SetActiveSkill(SkillNode.Shockwave);
            session.StartRun();
            yield return null;
            session.SetZombiePositionForTests(new Vector2(1f, 0f));
            var healthBefore = session.ZombieHealthForTests;

            Assert.That(session.ActivateActiveSkillForTests(), Is.True);
            yield return null;

            Assert.That(session.ZombieHealthForTests, Is.LessThan(healthBefore));
            Assert.That(session.ZombiePosition.x, Is.GreaterThan(1.5f));
        }

        [UnityTest]
        public IEnumerator Active_skill_selection_works_from_the_character_sheet()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.Profile.Level = 5;
            session.Profile.SetSkillRank(SkillNode.Dash, 1);
            session.ShowCharacterSheet();
            yield return null;

            Assert.That(session.TrySelectActiveSkill(SkillNode.Dash), Is.True);
            Assert.That(session.Profile.ActiveSkill, Is.EqualTo(SkillNode.Dash));

            session.CloseCharacterSheet();
            session.SaveNow();
            yield return null;
            Assert.That(File.Exists(_path), Is.True);
        }

        [UnityTest]
        public IEnumerator Zero_rank_active_skill_cannot_be_selected()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.ShowArmory();
            yield return null;

            Assert.That(session.TrySelectActiveSkill(SkillNode.Dash), Is.False);
            Assert.That(session.Profile.ActiveSkill, Is.EqualTo(SkillNode.Turret));
        }

        private static GameBootstrap CreateBootstrap()
        {
            var gameObject = new GameObject("Test Bootstrap");
            return gameObject.AddComponent<GameBootstrap>();
        }
    }
}
