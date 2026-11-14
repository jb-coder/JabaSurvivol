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
    public sealed class WeaponVisualPlayModeTests
    {
        private string _directory;
        private string _path;
        private GameBootstrap _bootstrap;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "jabasurvivol-weapon-visuals", Guid.NewGuid().ToString("N"));
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
        public IEnumerator Pistol_uses_authored_sprite_projectile_and_muzzle_flash()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.StartRun();
            yield return null;

            Assert.That(session.PlayerWeaponSpriteName, Is.EqualTo("pistol"));

            session.FireForTests();
            yield return null;

            Assert.That(session.ActiveBulletCount, Is.GreaterThan(0));
            Assert.That(session.FirstBulletSpriteName, Is.EqualTo("pistol"));
            Assert.That(session.ActiveMuzzleFlashes, Is.GreaterThan(0));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(session.FirstBulletRotation.eulerAngles.z, 0f)), Is.LessThan(1f));

            yield return new WaitForSeconds(0.25f);
            Assert.That(session.ActiveMuzzleFlashes, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Smg_and_shotgun_swap_visual_and_projectile_profiles()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;

            session.Profile.AddSupplies(WeaponCatalog.Get(WeaponId.Smg).UnlockCost);
            Assert.That(session.TryPurchaseWeapon(WeaponId.Smg), Is.EqualTo(ArmoryPurchaseResult.Purchased));
            Assert.That(session.TryEquipWeapon(WeaponId.Smg), Is.True);
            session.StartRun();
            yield return null;

            Assert.That(session.PlayerWeaponSpriteName, Is.EqualTo("smg"));
            session.FireForTests();
            yield return null;
            Assert.That(session.FirstBulletSpriteName, Is.EqualTo("smg"));

            session.Profile.AddSupplies(WeaponCatalog.Get(WeaponId.Shotgun).UnlockCost);
            Assert.That(session.TryPurchaseWeapon(WeaponId.Shotgun), Is.EqualTo(ArmoryPurchaseResult.Purchased));
            Assert.That(session.TryEquipWeapon(WeaponId.Shotgun), Is.True);
            session.StartRun();
            yield return null;

            Assert.That(session.PlayerWeaponSpriteName, Is.EqualTo("shotgun"));
            session.FireForTests();
            yield return null;
            Assert.That(session.FirstBulletSpriteName, Is.EqualTo("pellet"));
            Assert.That(session.ActiveBulletCount, Is.GreaterThan(1));
        }

        [UnityTest]
        public IEnumerator Impacts_and_death_bursts_are_pooled_and_clear()
        {
            var parent = new GameObject("Effect Test Parent");
            var impacts = new ImpactEffectPool(parent.transform);
            var bullets = new BulletPool(parent.transform, impacts, 4);
            var deaths = new DeathEffectPool(parent.transform);

            try
            {
                bullets.Spawn(Vector2.zero, Vector2.right, 1f, 1f, 1f, BulletStyle.Rifle);
                var damage = bullets.UpdateAndCheck(0.016f, true, Vector2.zero, 0.5f);

                Assert.That(damage, Is.EqualTo(1f));
                Assert.That(impacts.ActiveCount, Is.EqualTo(1));
                Assert.That(bullets.ActiveCount, Is.EqualTo(0));

                impacts.Update(1f);
                Assert.That(impacts.ActiveCount, Is.EqualTo(0));

                deaths.Spawn(Vector2.zero, Color.white, 1f);
                Assert.That(deaths.ActiveCount, Is.EqualTo(1));
                Assert.That(parent.GetComponentsInChildren<SpriteRenderer>(true).Length, Is.GreaterThanOrEqualTo(2));

                deaths.Update(1f);
                Assert.That(deaths.ActiveCount, Is.EqualTo(0));
            }
            finally
            {
                bullets.Clear();
                impacts.Clear();
                deaths.Clear();
                UnityEngine.Object.DestroyImmediate(parent);
            }

            yield return null;
        }

        private static GameBootstrap CreateBootstrap()
        {
            var gameObject = new GameObject("Test Bootstrap");
            return gameObject.AddComponent<GameBootstrap>();
        }
    }
}
