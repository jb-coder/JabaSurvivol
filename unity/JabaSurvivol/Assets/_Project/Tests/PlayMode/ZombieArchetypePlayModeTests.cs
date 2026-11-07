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
    public sealed class ZombieArchetypePlayModeTests
    {
        private string _directory;
        private string _path;
        private GameBootstrap _bootstrap;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "jabasurvivol-archetypes", Guid.NewGuid().ToString("N"));
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
        public IEnumerator Round_one_spawns_only_walkers()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.StartRun();
            yield return null;

            Assert.That(session.CurrentZombieArchetype, Is.EqualTo(ZombieArchetype.Walker));
            Assert.That(session.ZombiePoolCount, Is.EqualTo(ZombieCatalog.All.Count));
            Assert.That(session.EliteRound, Is.False);

            session.DamageZombieForTests(100000f);
            yield return null;
            session.SpawnZombieForTests();
            yield return null;

            Assert.That(session.CurrentZombieArchetype, Is.EqualTo(ZombieArchetype.Walker));
        }

        [UnityTest]
        public IEnumerator Elite_round_uses_scaled_values_and_rewards()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.StartRunForTests(GameBalance.EliteRoundInterval);
            yield return null;

            var encounter = session.Run.RoundModel.Definition.GetEncounter(session.CurrentZombieArchetype);
            Assert.That(session.EliteRound, Is.True);
            Assert.That(session.RoundModifierVisible, Is.True);
            Assert.That(session.CurrentZombieMaxHealth, Is.EqualTo(encounter.MaxHealth).Within(0.001f));

            session.DamageZombieForTests(100000f);
            yield return null;

            Assert.That(session.Profile.Supplies, Is.EqualTo(encounter.Supplies));
        }

        [UnityTest]
        public IEnumerator Archetype_rewards_survive_death()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.StartRunForTests(GameBalance.BruteSpawnRound);
            yield return null;

            var encounter = session.Run.RoundModel.Definition.GetEncounter(session.CurrentZombieArchetype);
            session.DamageZombieForTests(100000f);
            yield return null;
            session.ApplyDamage(GameBalance.PlayerMaxHealth * 5f);
            yield return null;

            Assert.That(session.State, Is.EqualTo(GameSessionState.GameOver));
            Assert.That(session.Profile.Supplies, Is.EqualTo(encounter.Supplies));
        }

        [UnityTest]
        public IEnumerator Spawn_plan_matches_the_round_state()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.StartRunForTests(GameBalance.BruteSpawnRound);
            yield return null;

            var plan = session.Run.RoundModel.SpawnPlan;
            for (var i = 0; i < plan.Count; i++)
            {
                Assert.That(session.CurrentZombieArchetype, Is.EqualTo(plan[i]));
                session.DamageZombieForTests(100000f);
                yield return null;

                if (i < plan.Count - 1)
                {
                    session.SpawnZombieForTests();
                    yield return null;
                }
            }
        }

        [UnityTest]
        public IEnumerator Archetype_actor_and_death_effect_are_pooled()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.StartRun();
            yield return null;

            var actorId = session.CurrentZombieActorId;
            Assert.That(actorId, Is.Not.EqualTo(0));

            session.DamageZombieForTests(100000f);
            yield return null;

            Assert.That(session.ActiveDeathEffects, Is.GreaterThan(0));

            session.SpawnZombieForTests();
            yield return null;

            Assert.That(session.CurrentZombieActorId, Is.EqualTo(actorId));
            Assert.That(session.ZombiePoolCount, Is.EqualTo(ZombieCatalog.All.Count));
        }

        private static GameBootstrap CreateBootstrap()
        {
            var gameObject = new GameObject("Test Bootstrap");
            return gameObject.AddComponent<GameBootstrap>();
        }
    }
}
