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
    public sealed class WeaponAmmoPlayModeTests
    {
        private string _directory;
        private string _path;
        private GameBootstrap _bootstrap;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "jabasurvivol-ammo", Guid.NewGuid().ToString("N"));
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
        public IEnumerator Firing_consumes_ammo_and_manual_reload_refills()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.StartRun();
            yield return null;

            var magazine = session.Magazine;
            session.FireForTests();
            yield return null;
            Assert.That(session.Magazine, Is.EqualTo(magazine - 1));

            Assert.That(session.ReloadWeapon(), Is.True);
            Assert.That(session.IsReloading, Is.True);

            var reload = WeaponCatalog.Get(WeaponId.Pistol).ReloadSeconds;
            yield return new WaitForSeconds(reload + 0.3f);

            Assert.That(session.IsReloading, Is.False);
            Assert.That(session.Magazine, Is.EqualTo(session.EquippedWeaponStats.MagazineSize));
        }

        [UnityTest]
        public IEnumerator Empty_magazine_stops_firing_and_auto_reloads()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            var previousScale = Time.timeScale;
            Time.timeScale = 5f;
            try
            {
                session.SetInputForTests(new ScriptedInput
                {
                    Frame = new InputFrame { Aim = Vector2.right, FireHeld = true }
                });
                session.StartRun();
                yield return null;

                var sawReloading = false;
                for (var i = 0; i < 240 && !sawReloading; i++)
                {
                    yield return null;
                    sawReloading = session.IsReloading;
                }

                Assert.That(sawReloading, Is.True);

                var refilled = false;
                for (var i = 0; i < 240 && !refilled; i++)
                {
                    yield return null;
                    refilled = !session.IsReloading &&
                               session.Magazine == session.MagazineSize;
                }

                Assert.That(refilled, Is.True);
            }
            finally
            {
                Time.timeScale = previousScale;
            }
        }

        [UnityTest]
        public IEnumerator Switch_weapon_preserves_each_magazine()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.Profile.AddSupplies(WeaponCatalog.Get(WeaponId.Smg).UnlockCost);
            session.TryPurchaseWeapon(WeaponId.Smg);
            session.TryEquipWeapon(WeaponId.Smg);
            session.StartRun();
            yield return null;

            Assert.That(session.CurrentWeapon, Is.EqualTo(WeaponId.Smg));
            var smgMagazine = session.Magazine;
            session.FireForTests();
            session.FireForTests();
            yield return null;
            Assert.That(session.Magazine, Is.EqualTo(smgMagazine - 2));

            Assert.That(session.SwitchWeapon(), Is.True);
            yield return null;
            Assert.That(session.CurrentWeapon, Is.EqualTo(WeaponId.Pistol));

            session.FireForTests();
            yield return null;
            Assert.That(session.SwitchWeapon(), Is.True);
            yield return null;
            Assert.That(session.CurrentWeapon, Is.EqualTo(WeaponId.Smg));
            Assert.That(session.Magazine, Is.EqualTo(smgMagazine - 2));
        }

        [UnityTest]
        public IEnumerator Death_resets_magazine_and_reserve()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.Profile.AddSupplies(WeaponCatalog.Get(WeaponId.Rifle).UnlockCost);
            session.TryPurchaseWeapon(WeaponId.Rifle);
            session.TryEquipWeapon(WeaponId.Rifle);
            session.StartRun();
            yield return null;

            var initialReserve = session.Reserve;
            var magazine = session.Magazine;
            session.FireForTests();
            session.FireForTests();
            yield return null;
            Assert.That(session.Magazine, Is.EqualTo(magazine - 2));

            session.ApplyDamage(GameBalance.PlayerMaxHealth * 5f);
            yield return null;
            session.RestartRun();
            yield return null;

            Assert.That(session.Magazine, Is.EqualTo(session.EquippedWeaponStats.MagazineSize));
            Assert.That(session.Reserve, Is.EqualTo(initialReserve));
        }

        [UnityTest]
        public IEnumerator Reload_is_rejected_when_the_magazine_is_full()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.StartRun();
            yield return null;

            Assert.That(session.ReloadWeapon(), Is.False);
            Assert.That(session.IsReloading, Is.False);
        }

        [UnityTest]
        public IEnumerator Mid_run_upgrade_updates_reload_and_magazine_capacity()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.Profile.AddSupplies(2000);
            session.StartRun();
            yield return null;
            session.PauseGame();
            yield return null;

            var reloadBefore = session.CurrentReloadDuration;
            var magazineBefore = session.MagazineSize;

            Assert.That(
                session.TryPurchaseWeaponUpgrade(WeaponUpgradeStat.ReloadSpeed),
                Is.EqualTo(WeaponUpgradePurchaseResult.Purchased));
            Assert.That(session.CurrentReloadDuration, Is.LessThan(reloadBefore));

            Assert.That(
                session.TryPurchaseWeaponUpgrade(WeaponUpgradeStat.MagazineSize),
                Is.EqualTo(WeaponUpgradePurchaseResult.Purchased));
            Assert.That(session.MagazineSize, Is.GreaterThan(magazineBefore));

            session.ResumeGame();
            yield return null;
            Assert.That(session.State, Is.EqualTo(GameSessionState.Playing));
        }

        [UnityTest]
        public IEnumerator Armory_upgrade_targets_the_run_weapon_while_paused()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.Profile.AddSupplies(3000);
            session.TryPurchaseWeapon(WeaponId.Smg);
            session.TryEquipWeapon(WeaponId.Smg);
            session.StartRun();
            yield return null;
            Assert.That(session.SwitchWeapon(), Is.True);
            Assert.That(session.CurrentWeapon, Is.EqualTo(WeaponId.Pistol));

            session.PauseGame();
            yield return null;
            session.ShowArmory();
            yield return null;

            Assert.That(
                session.TryPurchaseWeaponUpgrade(WeaponUpgradeStat.ReloadSpeed),
                Is.EqualTo(WeaponUpgradePurchaseResult.Purchased));
            Assert.That(
                session.Profile.GetWeaponUpgradeTier(WeaponId.Pistol, WeaponUpgradeStat.ReloadSpeed),
                Is.EqualTo(1));
            Assert.That(
                session.Profile.GetWeaponUpgradeTier(WeaponId.Smg, WeaponUpgradeStat.ReloadSpeed),
                Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Armory_upgrade_after_returning_to_menu_uses_the_profile_weapon()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.Profile.AddSupplies(3000);
            session.TryPurchaseWeapon(WeaponId.Smg);
            session.TryEquipWeapon(WeaponId.Smg);
            session.StartRun();
            yield return null;
            Assert.That(session.SwitchWeapon(), Is.True);
            session.ReturnToMenu();
            yield return null;

            session.ShowArmory();
            yield return null;

            Assert.That(
                session.TryPurchaseWeaponUpgrade(WeaponUpgradeStat.ReloadSpeed),
                Is.EqualTo(WeaponUpgradePurchaseResult.Purchased));
            Assert.That(
                session.Profile.GetWeaponUpgradeTier(WeaponId.Smg, WeaponUpgradeStat.ReloadSpeed),
                Is.EqualTo(1));
            Assert.That(
                session.Profile.GetWeaponUpgradeTier(WeaponId.Pistol, WeaponUpgradeStat.ReloadSpeed),
                Is.EqualTo(0));
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
