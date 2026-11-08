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
    public sealed class BossPlayModeTests
    {
        private string _directory;
        private string _path;
        private GameBootstrap _bootstrap;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "jabasurvivol-boss", Guid.NewGuid().ToString("N"));
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
                Directory.Delete(_directory, true);
            }
        }

        [UnityTest]
        public IEnumerator Boss_spawns_after_regular_enemies_and_shows_the_hud()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.StartRunForTests(GameBalance.BossRound);
            yield return null;

            yield return ClearRegularEnemies(session);

            Assert.That(session.BossSpawnPending, Is.True);
            Assert.That(session.BossActive, Is.False);
            Assert.That(session.RoundPhase, Is.EqualTo(RoundPhase.Active));

            session.SpawnBossForTests();
            yield return null;

            Assert.That(session.BossActive, Is.True);
            Assert.That(session.BossVisible, Is.True);
            Assert.That(session.BossHealth, Is.EqualTo(BossCatalog.BossDefinition.MaxHealth));
            Assert.That(session.BossMaxHealth, Is.EqualTo(BossCatalog.BossDefinition.MaxHealth));
        }

        [UnityTest]
        public IEnumerator Boss_kill_enters_victory_and_rewards_the_profile()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.StartRunForTests(GameBalance.BossRound);
            yield return null;
            yield return ClearRegularEnemies(session);
            session.SpawnBossForTests();
            yield return null;

            var suppliesBefore = session.Profile.Supplies;
            var xpBefore = session.Profile.Xp;
            var levelBefore = session.Profile.Level;

            session.DamageBossForTests(BossCatalog.BossDefinition.MaxHealth);
            yield return null;

            Assert.That(session.State, Is.EqualTo(GameSessionState.Victory));
            Assert.That(session.IsVictory, Is.True);
            Assert.That(session.VictoryVisible, Is.True);
            Assert.That(session.Profile.Supplies, Is.EqualTo(suppliesBefore + 1000));
            Assert.That(
                session.Profile.Level > levelBefore || session.Profile.Xp > xpBefore,
                Is.True);
            Assert.That(session.Profile.HighestRound, Is.EqualTo(GameBalance.BossRound));
            Assert.That(File.Exists(_path), Is.True);
        }

        [UnityTest]
        public IEnumerator Boss_enrages_and_telegraphs_its_slam()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.StartRunForTests(GameBalance.BossRound);
            yield return null;
            yield return ClearRegularEnemies(session);
            session.SpawnBossForTests();
            yield return null;

            session.DamageBossForTests(BossCatalog.BossDefinition.MaxHealth * 0.6f);
            yield return null;
            Assert.That(session.BossEnraged, Is.True);
            Assert.That(session.BossHealth, Is.LessThan(session.BossMaxHealth));

            session.SetBossAttackCooldownForTests(0f);
            yield return null;
            Assert.That(session.BossTelegraphing, Is.True);
        }

        [UnityTest]
        public IEnumerator Death_during_the_boss_round_keeps_persistent_progress()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.StartRunForTests(GameBalance.BossRound);
            yield return null;
            yield return ClearRegularEnemies(session);
            session.SpawnBossForTests();
            yield return null;

            var supplies = session.Profile.Supplies;
            session.ApplyDamage(GameBalance.PlayerMaxHealth * 10f);
            yield return null;

            Assert.That(session.State, Is.EqualTo(GameSessionState.GameOver));
            Assert.That(session.IsVictory, Is.False);
            Assert.That(session.Profile.Supplies, Is.EqualTo(supplies));
        }

        [UnityTest]
        public IEnumerator Victory_rewards_are_not_granted_twice()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.StartRunForTests(GameBalance.BossRound);
            yield return null;
            yield return ClearRegularEnemies(session);
            session.SpawnBossForTests();
            yield return null;

            session.DamageBossForTests(BossCatalog.BossDefinition.MaxHealth);
            yield return null;
            var supplies = session.Profile.Supplies;

            session.DamageBossForTests(BossCatalog.BossDefinition.MaxHealth);
            yield return null;

            Assert.That(session.State, Is.EqualTo(GameSessionState.Victory));
            Assert.That(session.Profile.Supplies, Is.EqualTo(supplies));
        }

        [UnityTest]
        public IEnumerator Boss_hud_returns_after_the_armory_from_pause()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.StartRunForTests(GameBalance.BossRound);
            yield return null;
            yield return ClearRegularEnemies(session);
            session.SpawnBossForTests();
            yield return null;

            session.PauseGame();
            yield return null;
            session.ShowArmory();
            yield return null;
            Assert.That(session.ArmoryVisible, Is.True);

            session.CloseArmory();
            yield return null;

            Assert.That(session.State, Is.EqualTo(GameSessionState.Paused));
            Assert.That(session.HudVisible, Is.True);
            Assert.That(session.BossVisible, Is.True);
            Assert.That(session.BossActive, Is.True);
        }

        private static IEnumerator ClearRegularEnemies(GameSession session)
        {
            var budget = session.SpawnBudget;
            for (var i = 0; i < budget; i++)
            {
                session.DamageZombieForTests(100000f);
                yield return null;

                if (i < budget - 1)
                {
                    session.SpawnZombieForTests();
                    yield return null;
                }
            }
        }

        private static GameBootstrap CreateBootstrap()
        {
            var gameObject = new GameObject("Test Bootstrap");
            return gameObject.AddComponent<GameBootstrap>();
        }
    }
}
