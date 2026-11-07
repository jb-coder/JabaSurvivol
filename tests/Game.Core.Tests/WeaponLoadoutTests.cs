namespace JabaSurvivol.Core.Tests;

public sealed class WeaponLoadoutTests
{
    [Fact]
    public void Pistol_has_infinite_reserve_and_a_full_magazine()
    {
        var run = new RunState(GameBalance.PlayerMaxHealth);

        Assert.Equal(WeaponId.Pistol, run.CurrentWeapon);
        Assert.True(run.CurrentWeaponState.HasInfiniteReserve);
        Assert.Equal(WeaponCatalog.Get(WeaponId.Pistol).MagazineSize, run.Magazine);
        Assert.Equal(0, run.Reserve);

        for (var i = 0; i < run.CurrentWeaponState.MagazineSize; i++)
        {
            Assert.True(run.TryFireCurrentWeapon());
        }

        Assert.False(run.TryFireCurrentWeapon());
        Assert.Equal(0, run.Magazine);
    }

    [Fact]
    public void Reload_refills_from_reserve_and_uses_the_effective_time()
    {
        var profile = CreateProfileWith(WeaponId.Rifle);
        var run = new RunState(
            GameBalance.PlayerMaxHealth,
            profile: profile,
            ammoCapacityMultiplier: 1f,
            reloadSpeedMultiplier: 2f);
        var baseReload = WeaponCatalog.Get(WeaponId.Rifle).ReloadSeconds;

        for (var i = 0; i < 4; i++)
        {
            Assert.True(run.TryFireCurrentWeapon());
        }

        Assert.True(run.StartReload());
        Assert.True(run.IsReloading);
        Assert.Equal(baseReload / 2f, run.CurrentWeaponState.ReloadDuration, precision: 4);

        run.TickWeapons((baseReload / 2f) - 0.01f);
        Assert.True(run.IsReloading);

        run.TickWeapons(0.02f);
        Assert.False(run.IsReloading);
        Assert.Equal(run.CurrentWeaponState.MagazineSize, run.Magazine);
        Assert.Equal(WeaponCatalog.Get(WeaponId.Rifle).ReserveAmmo - 4, run.Reserve);
    }

    [Fact]
    public void Engineering_increases_magazine_capacity()
    {
        var profile = CreateProfileWith(WeaponId.Smg);
        var run = new RunState(
            GameBalance.PlayerMaxHealth,
            profile: profile,
            ammoCapacityMultiplier: 1.25f);

        Assert.Equal(38, run.CurrentWeaponState.MagazineSize);
        Assert.Equal(38, run.Magazine);

        run.ApplyAmmoStats(1.5f, 1f);
        Assert.Equal(45, run.CurrentWeaponState.MagazineSize);
        Assert.Equal(38, run.Magazine);
    }

    [Fact]
    public void Shotgun_consumes_one_shell_per_trigger_pull()
    {
        var profile = CreateProfileWith(WeaponId.Shotgun);
        var run = new RunState(GameBalance.PlayerMaxHealth, profile: profile);
        var magazine = run.Magazine;

        Assert.True(run.TryFireCurrentWeapon());

        Assert.Equal(magazine - 1, run.Magazine);
        Assert.Equal(6, WeaponCatalog.Get(WeaponId.Shotgun).PelletCount);
    }

    [Fact]
    public void Switching_keeps_each_weapon_magazine_state()
    {
        var profile = CreateProfileWith(WeaponId.Smg);
        profile.UnlockWeapon(WeaponId.Shotgun);
        var run = new RunState(GameBalance.PlayerMaxHealth, profile: profile);
        var smgMagazine = run.Magazine;
        var shotgunMagazine = WeaponCatalog.Get(WeaponId.Shotgun).MagazineSize;

        for (var i = 0; i < 5; i++)
        {
            run.TryFireCurrentWeapon();
        }

        Assert.True(run.SwitchToWeapon(WeaponId.Shotgun));
        Assert.Equal(WeaponId.Shotgun, run.CurrentWeapon);
        Assert.Equal(shotgunMagazine, run.Magazine);

        run.TryFireCurrentWeapon();
        run.TryFireCurrentWeapon();
        Assert.Equal(shotgunMagazine - 2, run.Magazine);

        Assert.True(run.SwitchToWeapon(WeaponId.Smg));
        Assert.Equal(smgMagazine - 5, run.Magazine);
    }

    [Fact]
    public void SwitchNext_cycles_unlocked_weapons()
    {
        var profile = CreateProfileWith(WeaponId.Smg);
        profile.UnlockWeapon(WeaponId.Shotgun);
        var run = new RunState(GameBalance.PlayerMaxHealth, profile: profile);

        Assert.True(run.SwitchNextWeapon());
        Assert.Equal(WeaponId.Shotgun, run.CurrentWeapon);
        Assert.True(run.SwitchNextWeapon());
        Assert.Equal(WeaponId.Pistol, run.CurrentWeapon);
    }

    [Fact]
    public void Ammo_crate_fills_every_finite_reserve_and_ignores_the_pistol()
    {
        var profile = CreateProfileWith(WeaponId.Smg);
        profile.UnlockWeapon(WeaponId.Shotgun);
        var run = new RunState(GameBalance.PlayerMaxHealth, profile: profile);
        var smgReserve = WeaponCatalog.Get(WeaponId.Smg).ReserveAmmo;
        var shotgunReserve = WeaponCatalog.Get(WeaponId.Shotgun).ReserveAmmo;

        var applied = run.AddAmmo(24);

        Assert.Equal(48, applied);
        Assert.Equal(smgReserve + 24, Loadout(profile, run, WeaponId.Smg).Reserve);
        Assert.Equal(shotgunReserve + 24, Loadout(profile, run, WeaponId.Shotgun).Reserve);
    }

    [Fact]
    public void Restart_resets_magazines_and_reserves()
    {
        var profile = CreateProfileWith(WeaponId.Rifle);
        var run = new RunState(GameBalance.PlayerMaxHealth, profile: profile);
        var initialReserve = run.Reserve;

        for (var i = 0; i < 3; i++)
        {
            run.TryFireCurrentWeapon();
        }

        run.AddAmmo(10);
        run.DamagePlayer(GameBalance.PlayerMaxHealth * 2f);
        run.Restart();

        Assert.Equal(run.CurrentWeaponState.MagazineSize, run.Magazine);
        Assert.Equal(initialReserve, run.Reserve);
        Assert.False(run.IsReloading);
    }

    [Fact]
    public void Reload_is_rejected_when_the_reserve_is_empty()
    {
        var profile = CreateProfileWith(WeaponId.Smg);
        var run = new RunState(GameBalance.PlayerMaxHealth, profile: profile);

        while (run.Reserve > 0)
        {
            while (run.TryFireCurrentWeapon())
            {
            }

            Assert.True(run.StartReload());
            run.TickWeapons(1000f);
        }

        while (run.TryFireCurrentWeapon())
        {
        }

        Assert.Equal(0, run.Reserve);
        Assert.False(run.StartReload());
    }

    private static WeaponRuntimeState Loadout(Profile profile, RunState run, WeaponId weapon)
    {
        for (var i = 0; i < run.Loadout.States.Count; i++)
        {
            if (run.Loadout.States[i].Id == weapon)
            {
                return run.Loadout.States[i];
            }
        }

        throw new ArgumentOutOfRangeException(nameof(weapon));
    }

    private static Profile CreateProfileWith(WeaponId weapon)
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
