using JabaSurvivol.Core;
using NUnit.Framework;

namespace JabaSurvivol.Tests.EditMode
{
    public sealed class WeaponAmmoEditModeTests
    {
        [Test]
        public void Firing_consumes_magazine_and_reload_uses_effective_time()
        {
            var profile = CreateProfile(WeaponId.Rifle);
            var run = new RunState(GameBalance.PlayerMaxHealth, profile: profile, reloadSpeedMultiplier: 2f);
            var magazine = run.Magazine;

            Assert.That(run.TryFireCurrentWeapon(), Is.True);
            Assert.That(run.Magazine, Is.EqualTo(magazine - 1));

            Assert.That(run.StartReload(), Is.True);
            run.TickWeapons(WeaponCatalog.Get(WeaponId.Rifle).ReloadSeconds / 2f);

            Assert.That(run.IsReloading, Is.False);
            Assert.That(run.Magazine, Is.EqualTo(run.CurrentWeaponState.MagazineSize));
        }

        [Test]
        public void Switching_preserves_per_weapon_magazine_state()
        {
            var profile = CreateProfile(WeaponId.Smg);
            profile.UnlockWeapon(WeaponId.Shotgun);
            var run = new RunState(GameBalance.PlayerMaxHealth, profile: profile);
            var smgMagazine = run.Magazine;

            run.TryFireCurrentWeapon();
            run.TryFireCurrentWeapon();
            Assert.That(run.SwitchToWeapon(WeaponId.Shotgun), Is.True);
            run.TryFireCurrentWeapon();
            Assert.That(run.SwitchToWeapon(WeaponId.Smg), Is.True);

            Assert.That(run.Magazine, Is.EqualTo(smgMagazine - 2));
        }

        [Test]
        public void Ammo_crate_refills_reserve_and_death_resets_it()
        {
            var profile = CreateProfile(WeaponId.Smg);
            var run = new RunState(GameBalance.PlayerMaxHealth, profile: profile);
            var initialReserve = run.Reserve;

            Assert.That(run.AddAmmo(GameBalance.AmmoCrateAmount), Is.EqualTo(GameBalance.AmmoCrateAmount));
            Assert.That(run.Reserve, Is.EqualTo(initialReserve + GameBalance.AmmoCrateAmount));

            run.DamagePlayer(GameBalance.PlayerMaxHealth * 2f);
            run.Restart();

            Assert.That(run.Reserve, Is.EqualTo(initialReserve));
            Assert.That(run.Magazine, Is.EqualTo(run.CurrentWeaponState.MagazineSize));
        }

        private static Profile CreateProfile(WeaponId weapon)
        {
            var profile = new Profile();
            if (weapon != WeaponId.Pistol)
            {
                profile.UnlockWeapon(weapon);
                profile.EquipWeapon(weapon);
            }

            return profile;
        }
    }
}
