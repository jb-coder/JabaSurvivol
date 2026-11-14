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
    public sealed class CharacterVisualPlayModeTests
    {
        private string _directory;
        private string _path;
        private GameBootstrap _bootstrap;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "jabasurvivol-visuals", Guid.NewGuid().ToString("N"));
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
        public IEnumerator Player_visual_has_authored_parts_and_tracks_aim()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.StartRun();
            yield return null;

            var visual = session.PlayerVisual;
            Assert.That(visual, Is.Not.Null);
            Assert.That(visual.Renderers.Length, Is.GreaterThanOrEqualTo(6));
            Assert.That(visual.AimPivot, Is.Not.Null);
            Assert.That(visual.WeaponMount, Is.Not.Null);

            var weapon = visual.WeaponMount.GetComponent<SpriteRenderer>();
            Assert.That(weapon, Is.Not.Null);
            Assert.That(weapon.sprite, Is.Not.Null);

            visual.SetAim(Vector2.up);
            visual.Tick(0.016f);
            Assert.That(Mathf.DeltaAngle(visual.AimPivot.eulerAngles.z, 90f), Is.EqualTo(0f).Within(1f));
        }

        [UnityTest]
        public IEnumerator Zombie_visual_is_pooled_and_toggles_elite_aura()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.StartRun();
            yield return null;

            var visual = session.ZombieVisual;
            Assert.That(visual, Is.Not.Null);
            Assert.That(visual.Renderers.Length, Is.GreaterThanOrEqualTo(5));
            Assert.That(visual.EliteAuraVisible, Is.False);

            visual.SetElite(true);
            Assert.That(visual.EliteAuraVisible, Is.True);
            visual.SetElite(false);
            Assert.That(visual.EliteAuraVisible, Is.False);

            var actorId = session.CurrentZombieActorId;
            session.DamageZombieForTests(100000f);
            yield return null;
            session.SpawnZombieForTests();
            yield return null;

            Assert.That(session.CurrentZombieActorId, Is.EqualTo(actorId));
            Assert.That(session.ZombieVisual, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator Boss_visual_has_a_core_and_enrage_state()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.StartRunForTests(GameBalance.BossRound);
            yield return null;
            yield return ClearRegularEnemies(session);
            session.SpawnBossForTests();
            yield return null;

            var visual = session.BossVisual;
            Assert.That(visual, Is.Not.Null);
            Assert.That(visual.Renderers.Length, Is.GreaterThanOrEqualTo(6));
            Assert.That(visual.IsEnraged, Is.False);

            visual.SetEnraged(true);
            Assert.That(visual.IsEnraged, Is.True);
            visual.Tick(0.016f);
        }

        [UnityTest]
        public IEnumerator Turret_visual_has_authored_parts_when_deployed()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.Profile.SetSkillRank(SkillNode.Turret, 3);
            session.StartRun();
            yield return null;

            Assert.That(session.ActivateActiveSkillForTests(), Is.True);
            yield return null;

            var visual = session.TurretVisual;
            Assert.That(visual, Is.Not.Null);
            Assert.That(session.TurretVisible, Is.True);
            Assert.That(visual.Renderers.Length, Is.GreaterThanOrEqualTo(3));
            Assert.That(visual.AimPivot, Is.Not.Null);
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
