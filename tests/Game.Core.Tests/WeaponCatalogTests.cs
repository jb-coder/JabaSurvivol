namespace JabaSurvivol.Core.Tests;

public sealed class WeaponCatalogTests
{
    [Fact]
    public void Catalog_exposes_five_weapons_with_complete_combat_stats()
    {
        Assert.Equal(5, WeaponCatalog.All.Count);

        foreach (var definition in WeaponCatalog.All)
        {
            Assert.True(definition.Damage > 0f);
            Assert.True(definition.FireInterval > 0f);
            Assert.True(definition.MagazineSize > 0);
            Assert.True(definition.ReloadSeconds > 0f);
            Assert.True(definition.Range > 0f);
            Assert.True(definition.ProjectileSpeed > 0f);
            Assert.True(definition.PelletCount > 0);
            Assert.True(definition.UnlockCost >= 0);
            Assert.True(definition.HasInfiniteReserve || definition.ReserveAmmo > 0);
        }
    }

    [Fact]
    public void Pistol_is_the_starting_weapon_with_infinite_reserve()
    {
        var pistol = WeaponCatalog.Get(WeaponId.Pistol);
        var profile = new Profile();

        Assert.Equal(0, pistol.UnlockCost);
        Assert.True(pistol.HasInfiniteReserve);
        Assert.True(profile.HasWeapon(WeaponId.Pistol));
        Assert.Equal(WeaponId.Pistol, profile.EquippedWeapon);
    }

    [Fact]
    public void Unlock_costs_match_the_economy_design()
    {
        Assert.Equal(500, WeaponCatalog.Get(WeaponId.Smg).UnlockCost);
        Assert.Equal(900, WeaponCatalog.Get(WeaponId.Shotgun).UnlockCost);
        Assert.Equal(1200, WeaponCatalog.Get(WeaponId.Rifle).UnlockCost);
        Assert.Equal(2000, WeaponCatalog.Get(WeaponId.Lmg).UnlockCost);
    }

    [Fact]
    public void Armory_purchase_spends_supplies_and_unlocks_the_weapon()
    {
        var profile = new Profile();
        profile.AddSupplies(600);

        var result = ArmoryService.PurchaseWeapon(profile, WeaponId.Smg);

        Assert.Equal(ArmoryPurchaseResult.Purchased, result);
        Assert.Equal(100, profile.Supplies);
        Assert.True(profile.HasWeapon(WeaponId.Smg));
    }

    [Fact]
    public void Armory_purchase_rejects_insufficient_supplies_without_partial_state()
    {
        var profile = new Profile();
        profile.AddSupplies(499);

        var result = ArmoryService.PurchaseWeapon(profile, WeaponId.Smg);

        Assert.Equal(ArmoryPurchaseResult.InsufficientSupplies, result);
        Assert.Equal(499, profile.Supplies);
        Assert.False(profile.HasWeapon(WeaponId.Smg));
    }

    [Fact]
    public void Armory_purchase_does_not_charge_for_an_owned_weapon()
    {
        var profile = new Profile();
        profile.AddSupplies(600);
        ArmoryService.PurchaseWeapon(profile, WeaponId.Smg);
        profile.AddSupplies(50);

        var result = ArmoryService.PurchaseWeapon(profile, WeaponId.Smg);

        Assert.Equal(ArmoryPurchaseResult.AlreadyOwned, result);
        Assert.Equal(150, profile.Supplies);
    }

    [Fact]
    public void Equip_requires_an_unlocked_weapon()
    {
        var profile = new Profile();

        Assert.False(profile.EquipWeapon(WeaponId.Rifle));
        Assert.Equal(WeaponId.Pistol, profile.EquippedWeapon);
        Assert.False(ArmoryService.EquipWeapon(profile, WeaponId.Rifle));

        profile.UnlockWeapon(WeaponId.Rifle);

        Assert.True(ArmoryService.EquipWeapon(profile, WeaponId.Rifle));
        Assert.Equal(WeaponId.Rifle, profile.EquippedWeapon);
    }

    [Fact]
    public void Validate_restores_the_pistol_and_repairs_invalid_weapon_data()
    {
        var profile = new Profile
        {
            UnlockedWeapons = null!,
            EquippedWeapon = (WeaponId)99
        };

        profile.Validate();

        Assert.NotNull(profile.UnlockedWeapons);
        Assert.Contains(WeaponId.Pistol, profile.UnlockedWeapons);
        Assert.Equal(WeaponId.Pistol, profile.EquippedWeapon);
    }

    [Fact]
    public void Validate_removes_invalid_and_duplicate_weapon_entries()
    {
        var profile = new Profile
        {
            UnlockedWeapons = new List<WeaponId>
            {
                WeaponId.Smg,
                (WeaponId)99,
                WeaponId.Smg,
                WeaponId.Pistol
            },
            EquippedWeapon = WeaponId.Smg
        };

        profile.Validate();

        Assert.Equal(2, profile.UnlockedWeapons.Count);
        Assert.Contains(WeaponId.Pistol, profile.UnlockedWeapons);
        Assert.Contains(WeaponId.Smg, profile.UnlockedWeapons);
        Assert.Equal(WeaponId.Smg, profile.EquippedWeapon);
    }

    [Fact]
    public void Weapon_unlocks_and_equip_survive_death()
    {
        var profile = new Profile();
        profile.AddSupplies(1200);
        ArmoryService.PurchaseWeapon(profile, WeaponId.Rifle);
        ArmoryService.EquipWeapon(profile, WeaponId.Rifle);

        var run = new RunState(GameBalance.PlayerMaxHealth);
        run.DamagePlayer(GameBalance.PlayerMaxHealth * 2f);
        profile.RecordRunEnd(run);

        Assert.Equal(WeaponId.Rifle, profile.EquippedWeapon);
        Assert.True(profile.HasWeapon(WeaponId.Rifle));
        Assert.Equal(0, profile.Supplies);
    }

    [Fact]
    public void Armory_purchase_rejects_an_unknown_weapon()
    {
        var profile = new Profile();
        profile.AddSupplies(100);

        var result = ArmoryService.PurchaseWeapon(profile, (WeaponId)99);

        Assert.Equal(ArmoryPurchaseResult.UnknownWeapon, result);
        Assert.Equal(100, profile.Supplies);
    }

    [Fact]
    public void Catalog_rejects_unknown_weapons()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => WeaponCatalog.Get((WeaponId)99));
        Assert.Throws<ArgumentNullException>(() => ArmoryService.PurchaseWeapon(null!, WeaponId.Smg));
    }
}
