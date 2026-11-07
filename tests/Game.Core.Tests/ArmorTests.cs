namespace JabaSurvivol.Core.Tests;

public sealed class ArmorTests
{
    [Fact]
    public void Catalog_exposes_none_light_medium_and_heavy_tiers()
    {
        Assert.Equal(4, ArmorCatalog.All.Count);
        Assert.Equal(0, ArmorCatalog.Get(ArmorTier.None).AbsorbAmount);
        Assert.Equal(25, ArmorCatalog.Get(ArmorTier.Light).AbsorbAmount);
        Assert.Equal(50, ArmorCatalog.Get(ArmorTier.Medium).AbsorbAmount);
        Assert.Equal(90, ArmorCatalog.Get(ArmorTier.Heavy).AbsorbAmount);
        Assert.Equal(0, ArmorCatalog.Get(ArmorTier.Light).UnlockCost);
        Assert.Equal(800, ArmorCatalog.Get(ArmorTier.Medium).UnlockCost);
        Assert.Equal(1600, ArmorCatalog.Get(ArmorTier.Heavy).UnlockCost);
        Assert.Equal(5, ArmorCatalog.Get(ArmorTier.Heavy).MaxUpgradeTier);
    }

    [Fact]
    public void Armor_purchase_is_atomic_and_light_is_free()
    {
        var profile = new Profile();

        Assert.Equal(ArmorPurchaseResult.Purchased, ArmorService.PurchaseArmor(profile, ArmorTier.Light));
        Assert.True(profile.HasArmor(ArmorTier.Light));
        Assert.Equal(0, profile.Supplies);

        Assert.Equal(
            ArmorPurchaseResult.InsufficientSupplies,
            ArmorService.PurchaseArmor(profile, ArmorTier.Medium));
        Assert.False(profile.HasArmor(ArmorTier.Medium));
        Assert.Equal(0, profile.Supplies);

        profile.AddSupplies(800);
        Assert.Equal(ArmorPurchaseResult.Purchased, ArmorService.PurchaseArmor(profile, ArmorTier.Medium));
        Assert.Equal(0, profile.Supplies);
    }

    [Fact]
    public void Equip_requires_an_unlocked_armor_tier()
    {
        var profile = new Profile();

        Assert.False(ArmorService.EquipArmor(profile, ArmorTier.Heavy));
        Assert.Equal(ArmorTier.None, profile.EquippedArmor);

        ArmorService.PurchaseArmor(profile, ArmorTier.Light);

        Assert.True(ArmorService.EquipArmor(profile, ArmorTier.Light));
        Assert.Equal(ArmorTier.Light, profile.EquippedArmor);
    }

    [Fact]
    public void Armor_upgrades_spend_supplies_and_stop_at_max_tier()
    {
        var profile = new Profile();
        profile.AddSupplies(1000);
        ArmorService.PurchaseArmor(profile, ArmorTier.Light);

        var definition = ArmorCatalog.Get(ArmorTier.Light);
        var expectedCost = 0;
        for (var tier = 1; tier <= definition.MaxUpgradeTier; tier++)
        {
            expectedCost += definition.GetUpgradeCost(tier);
            Assert.Equal(
                ArmorUpgradePurchaseResult.Purchased,
                ArmorService.TryPurchaseUpgrade(profile, ArmorTier.Light));
        }

        Assert.Equal(
            ArmorUpgradePurchaseResult.MaxTier,
            ArmorService.TryPurchaseUpgrade(profile, ArmorTier.Light));
        Assert.Equal(definition.MaxUpgradeTier, profile.GetArmorUpgradeTier(ArmorTier.Light));
        Assert.Equal(1000 - expectedCost, profile.Supplies);
    }

    [Fact]
    public void Armor_upgrade_rejects_insufficient_supplies_and_locked_armor()
    {
        var profile = new Profile();
        ArmorService.PurchaseArmor(profile, ArmorTier.Light);

        Assert.Equal(
            ArmorUpgradePurchaseResult.InsufficientSupplies,
            ArmorService.TryPurchaseUpgrade(profile, ArmorTier.Light));
        Assert.Equal(0, profile.GetArmorUpgradeTier(ArmorTier.Light));

        Assert.Equal(
            ArmorUpgradePurchaseResult.NotOwned,
            ArmorService.TryPurchaseUpgrade(profile, ArmorTier.Heavy));
    }

    [Fact]
    public void Effective_absorb_applies_permanent_upgrade_tiers()
    {
        var profile = new Profile();
        ArmorService.PurchaseArmor(profile, ArmorTier.Light);
        profile.SetArmorUpgradeTier(ArmorTier.Light, 5);

        var stats = ArmorService.GetEffectiveStats(profile, ArmorTier.Light);

        Assert.Equal(38, stats.AbsorbAmount);
        Assert.Equal(25, stats.BaseAbsorbAmount);
        Assert.Equal(5, stats.UpgradeTier);
        Assert.True(stats.IsMaxUpgrade);
    }

    [Fact]
    public void Run_armor_absorbs_damage_before_health()
    {
        var run = new RunState(GameBalance.PlayerMaxHealth, maxArmor: 25);

        Assert.Equal(25, run.MaxArmor);
        Assert.Equal(25, run.Armor);

        Assert.False(run.DamagePlayer(10f));
        Assert.Equal(15, run.Armor);
        Assert.Equal(GameBalance.PlayerMaxHealth, run.Player.Health);

        Assert.False(run.DamagePlayer(20f));
        Assert.Equal(0, run.Armor);
        Assert.Equal(GameBalance.PlayerMaxHealth - 5f, run.Player.Health);
    }

    [Fact]
    public void Armor_absorbs_damage_that_would_kill_the_player()
    {
        var run = new RunState(GameBalance.PlayerMaxHealth, maxArmor: 10);

        Assert.True(run.DamagePlayer(GameBalance.PlayerMaxHealth + 10f));
        Assert.True(run.IsGameOver);
        Assert.Equal(0, run.Armor);
        Assert.False(run.Player.IsAlive);
    }

    [Fact]
    public void Free_armor_repairs_are_capped_at_the_maximum()
    {
        var run = new RunState(GameBalance.PlayerMaxHealth, maxArmor: 25);
        run.DamagePlayer(10f);

        Assert.Equal(10, run.AddArmor(20));
        Assert.Equal(25, run.Armor);
        Assert.Equal(0, run.AddArmor(20));
    }

    [Fact]
    public void Armor_plates_have_no_effect_without_equipped_armor()
    {
        var run = new RunState(GameBalance.PlayerMaxHealth);

        Assert.Equal(0, run.AddArmor(10));
        Assert.Equal(0, run.Armor);
        Assert.Equal(0, run.MaxArmor);
    }

    [Fact]
    public void Fractional_damage_absorbs_whole_armor_points()
    {
        var run = new RunState(GameBalance.PlayerMaxHealth, maxArmor: 10);

        Assert.False(run.DamagePlayer(5.7f));

        Assert.Equal(5, run.Armor);
        Assert.Equal(GameBalance.PlayerMaxHealth - 0.7f, run.Player.Health, 3);
    }

    [Fact]
    public void Restart_resets_armor_condition_but_keeps_profile_armor()
    {
        var profile = new Profile();
        profile.AddSupplies(1000);
        ArmorService.PurchaseArmor(profile, ArmorTier.Light);
        ArmorService.EquipArmor(profile, ArmorTier.Light);
        ArmorService.TryPurchaseUpgrade(profile, ArmorTier.Light);

        var stats = ArmorService.GetEffectiveStats(profile, ArmorTier.Light);
        var run = new RunState(GameBalance.PlayerMaxHealth, maxArmor: stats.AbsorbAmount);
        run.DamagePlayer(10f);
        run.DamagePlayer(GameBalance.PlayerMaxHealth * 2f);
        profile.RecordRunEnd(run);

        run.Restart();

        Assert.Equal(stats.AbsorbAmount, run.Armor);
        Assert.Equal(ArmorTier.Light, profile.EquippedArmor);
        Assert.Equal(1, profile.GetArmorUpgradeTier(ArmorTier.Light));
        Assert.True(profile.HasArmor(ArmorTier.Light));
    }

    [Fact]
    public void Validate_restores_none_when_armor_data_is_missing()
    {
        var profile = new Profile
        {
            UnlockedArmor = null!,
            EquippedArmor = ArmorTier.Heavy
        };

        profile.Validate();

        Assert.NotNull(profile.UnlockedArmor);
        Assert.Contains(ArmorTier.None, profile.UnlockedArmor);
        Assert.Equal(ArmorTier.None, profile.EquippedArmor);
    }

    [Fact]
    public void Validate_clamps_and_deduplicates_armor_upgrades()
    {
        var profile = new Profile
        {
            UnlockedArmor = new List<ArmorTier> { ArmorTier.None, ArmorTier.Light },
            EquippedArmor = (ArmorTier)99,
            ArmorUpgrades = new List<ArmorUpgrade>
            {
                new ArmorUpgrade(ArmorTier.Light, 99),
                new ArmorUpgrade(ArmorTier.Light, 2),
                new ArmorUpgrade(ArmorTier.Medium, 1),
                new ArmorUpgrade((ArmorTier)99, 1),
                new ArmorUpgrade(ArmorTier.None, 1)
            }
        };

        profile.Validate();

        Assert.Equal(ArmorTier.None, profile.EquippedArmor);
        Assert.Single(profile.ArmorUpgrades);
        Assert.Equal(5, profile.GetArmorUpgradeTier(ArmorTier.Light));
    }

    [Fact]
    public void Unknown_armor_is_rejected()
    {
        var profile = new Profile();

        Assert.Equal(
            ArmorPurchaseResult.UnknownArmor,
            ArmorService.PurchaseArmor(profile, (ArmorTier)99));
        Assert.Equal(
            ArmorUpgradePurchaseResult.UnknownArmor,
            ArmorService.TryPurchaseUpgrade(profile, (ArmorTier)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => ArmorCatalog.Get((ArmorTier)99));
    }
}
