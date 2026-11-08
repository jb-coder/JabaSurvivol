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
    public sealed class TurretPlayModeTests
    {
        private string _directory;
        private string _path;
        private GameBootstrap _bootstrap;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "jabasurvivol-turret", Guid.NewGuid().ToString("N"));
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
        public IEnumerator Zero_rank_turret_is_locked()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.StartRun();
            yield return null;

            Assert.That(session.ActiveSkillRank, Is.EqualTo(0));
            Assert.That(session.ActivateActiveSkillForTests(), Is.False);
            Assert.That(session.TurretVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator Turret_deploys_for_its_duration_and_starts_cooldown()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.Profile.SetSkillRank(SkillNode.Turret, 3);
            session.StartRun();
            yield return null;

            Assert.That(session.ActivateActiveSkillForTests(), Is.True);
            yield return null;

            Assert.That(session.TurretActive, Is.True);
            Assert.That(session.TurretVisible, Is.True);
            Assert.That(session.ActiveSkillRank, Is.EqualTo(3));
            Assert.That(session.ActiveSkillCooldownFraction, Is.GreaterThan(0f));
            Assert.That(session.ActivateActiveSkillForTests(), Is.False);
        }

        [UnityTest]
        public IEnumerator Turret_fires_and_damages_the_active_zombie()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.Profile.SetSkillRank(SkillNode.Turret, 5);
            var previousScale = Time.timeScale;
            Time.timeScale = 2f;
            try
            {
                session.StartRun();
                yield return null;
                Assert.That(session.ActivateActiveSkillForTests(), Is.True);
                yield return null;
                Assert.That(session.TurretActive, Is.True);
                Assert.That(session.ZombieActive, Is.True);

                var killsBefore = session.Run.Kills;
                for (var i = 0; i < 240 && session.Run.Kills == killsBefore; i++)
                {
                    yield return null;
                }

                Assert.That(session.TurretShotsFiredForTests, Is.GreaterThan(0));
                Assert.That(session.Run.Kills, Is.GreaterThan(killsBefore));
            }
            finally
            {
                Time.timeScale = previousScale;
            }
        }

        [UnityTest]
        public IEnumerator Turret_state_resets_on_death_and_new_run()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.Profile.SetSkillRank(SkillNode.Turret, 2);
            session.StartRun();
            yield return null;
            Assert.That(session.ActivateActiveSkillForTests(), Is.True);
            yield return null;

            session.ApplyDamage(GameBalance.PlayerMaxHealth * 5f);
            yield return null;
            Assert.That(session.TurretVisible, Is.False);

            session.RestartRun();
            yield return null;

            Assert.That(session.TurretActive, Is.False);
            Assert.That(session.TurretVisible, Is.False);
            Assert.That(session.ActiveSkillCooldownRemaining, Is.EqualTo(0f));
        }

        private static GameBootstrap CreateBootstrap()
        {
            var gameObject = new GameObject("Test Bootstrap");
            return gameObject.AddComponent<GameBootstrap>();
        }
    }
}
