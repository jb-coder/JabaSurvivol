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
    public sealed class WalkingSkeletonPlayModeTests
    {
        private string _directory;
        private string _path;
        private GameBootstrap _bootstrap;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "jabasurvivol-playmode", Guid.NewGuid().ToString("N"));
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
        public IEnumerator Bootstrap_starts_a_run_with_player_and_zombie()
        {
            _bootstrap = CreateBootstrap();
            yield return null;

            Assert.That(_bootstrap.Session, Is.Not.Null);

            _bootstrap.Session.StartRun();
            yield return null;

            Assert.That(_bootstrap.Session.State, Is.EqualTo(GameSessionState.Playing));
            Assert.That(_bootstrap.Session.PlayerActive, Is.True);
            Assert.That(_bootstrap.Session.ZombieActive, Is.True);
            Assert.That(_bootstrap.Session.Run.Player.Health, Is.EqualTo(GameBalance.PlayerMaxHealth));
        }

        [UnityTest]
        public IEnumerator Kill_awards_progress_and_death_keeps_it()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.StartRun();

            session.DamageZombieForTests(GameBalance.ZombieMaxHealth);
            yield return null;

            Assert.That(session.Profile.Supplies, Is.EqualTo(GameBalance.SuppliesPerKill));
            Assert.That(File.Exists(_path), Is.True);

            session.ApplyDamage(GameBalance.PlayerMaxHealth * 2f);
            yield return null;

            Assert.That(session.State, Is.EqualTo(GameSessionState.GameOver));
            Assert.That(session.PlayerActive, Is.False);
            Assert.That(session.Profile.Supplies, Is.EqualTo(GameBalance.SuppliesPerKill));
            Assert.That(session.Profile.TotalRuns, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Restart_resets_health_and_round_but_keeps_progress()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.StartRun();
            session.DamageZombieForTests(GameBalance.ZombieMaxHealth);
            session.ApplyDamage(GameBalance.PlayerMaxHealth * 2f);
            yield return null;

            var supplies = session.Profile.Supplies;
            session.RestartRun();
            yield return null;

            Assert.That(session.State, Is.EqualTo(GameSessionState.Playing));
            Assert.That(session.Run.Player.Health, Is.EqualTo(GameBalance.PlayerMaxHealth));
            Assert.That(session.Run.Round, Is.EqualTo(1));
            Assert.That(session.Run.Kills, Is.EqualTo(0));
            Assert.That(session.Profile.Supplies, Is.EqualTo(supplies));
        }

        [UnityTest]
        public IEnumerator Profile_survives_bootstrap_recreation()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            _bootstrap.Session.StartRun();
            _bootstrap.Session.DamageZombieForTests(GameBalance.ZombieMaxHealth);
            _bootstrap.Session.SaveNow();
            yield return null;

            var supplies = _bootstrap.Session.Profile.Supplies;
            UnityEngine.Object.DestroyImmediate(_bootstrap.gameObject);
            _bootstrap = null;
            yield return null;

            _bootstrap = CreateBootstrap();
            yield return null;

            Assert.That(_bootstrap.Session.Profile.Supplies, Is.EqualTo(supplies));
        }

        [UnityTest]
        public IEnumerator Full_round_clear_advances_round_and_keeps_progress()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.StartRun();
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

            Assert.That(session.RoundPhase, Is.EqualTo(RoundPhase.Cleared));
            Assert.That(session.RoundClearPending, Is.True);
            var suppliesAfterClear = session.Profile.Supplies;

            session.ContinueAfterRoundClear();
            yield return null;

            Assert.That(session.State, Is.EqualTo(GameSessionState.Playing));
            Assert.That(session.Run.Round, Is.EqualTo(2));
            Assert.That(session.EnemiesRemaining, Is.EqualTo(session.SpawnBudget));
            Assert.That(session.RoundPhase, Is.EqualTo(RoundPhase.Active));
            Assert.That(session.Profile.Supplies, Is.EqualTo(suppliesAfterClear));
            Assert.That(File.Exists(_path), Is.True);
        }

        [UnityTest]
        public IEnumerator Scripted_fire_spawns_bullets()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.SetInputForTests(new ScriptedInput
            {
                Frame = new InputFrame { Aim = Vector2.right, FireHeld = true }
            });
            session.StartRun();
            yield return null;
            yield return null;

            Assert.That(session.ActiveBullets, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator Pause_stops_the_run_and_resume_continues()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.StartRun();

            session.PauseGame();
            yield return null;
            Assert.That(session.State, Is.EqualTo(GameSessionState.Paused));

            session.ResumeGame();
            yield return null;
            Assert.That(session.State, Is.EqualTo(GameSessionState.Playing));
        }

        private static GameBootstrap CreateBootstrap()
        {
            var gameObject = new GameObject("Test Bootstrap");
            return gameObject.AddComponent<GameBootstrap>();
        }

        private sealed class ScriptedInput : IGameInput
        {
            public InputFrame Frame;

            public InputFrame Read(Vector2 playerWorldPosition)
            {
                return Frame;
            }
        }
    }
}
