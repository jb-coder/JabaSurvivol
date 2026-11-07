namespace JabaSurvivol.Core.Tests;

public sealed class WeaponUpgradeTests
{
    [Fact]
    public void Catalog_exposes_four_tracks_for_every_weapon()
    {
        foreach (var weapon in WeaponCatalog.All)
        {
            var tracks = WeaponUpgradeCatalog.GetTracks(weapon.Id);

            Assert.Equal(4, tracks.Count);
            foreach (var track in tracks)
            {
                Assert.Equal(5, track.MaxTier);
                Assert.True(track.BaseCost > 0);
                Assert.True(track.EffectPerTier > 0f);
                Assert.True(track.GetCost(5) > track.GetCost(1));
            }
        }
    }

    [Fact]
    public void Upgrade_purchase_spends_supplies_and_increases_the_tier()
    {
        var profile = new Profile();
        profile.AddSupplies(1000);
        var cost = WeaponUpgradeCatalog.Get(WeaponId.Pistol, WeaponUpgradeStat.Damage).GetCost(1);

        var result = WeaponUpgradeService.TryPurchase(profile, WeaponId.Pistol, WeaponUpgradeStat.Damage);

        Assert.Equal(WeaponUpgradePurchaseResult.Purchased, result);
        Assert.Equal(1000 - cost, profile.Supplies);
        Assert.Equal(1, profile.GetWeaponUpgradeTier(WeaponId.Pistol, WeaponUpgradeStat.Damage));
    }

    [Fact]
    public void Insufficient_supplies_does_not_spend_or_increment()
    {
        var profile = new Profile();
        profile.AddSupplies(10);

        var result = WeaponUpgradeService.TryPurchase(profile, WeaponId.Pistol, WeaponUpgradeStat.Damage);

        Assert.Equal(WeaponUpgradePurchaseResult.InsufficientSupplies, result);
        Assert.Equal(10, profile.Supplies);
        Assert.Equal(0, profile.GetWeaponUpgradeTier(WeaponId.Pistol, WeaponUpgradeStat.Damage));
    }

    [Fact]
    public void Max_tier_rejects_further_purchases_without_spending()
    {
        var profile = new Profile();
        profile.AddSupplies(1000);
        var track = WeaponUpgradeCatalog.Get(WeaponId.Pistol, WeaponUpgradeStat.Damage);
        var expectedCost = 0;
        for (var tier = 1; tier <= track.MaxTier; tier++)
        {
            expectedCost += track.GetCost(tier);
            Assert.Equal(
                WeaponUpgradePurchaseResult.Purchased,
                WeaponUpgradeService.TryPurchase(profile, WeaponId.Pistol, WeaponUpgradeStat.Damage));
        }

        var result = WeaponUpgradeService.TryPurchase(profile, WeaponId.Pistol, WeaponUpgradeStat.Damage);

        Assert.Equal(WeaponUpgradePurchaseResult.MaxTier, result);
        Assert.Equal(track.MaxTier, profile.GetWeaponUpgradeTier(WeaponId.Pistol, WeaponUpgradeStat.Damage));
        Assert.Equal(1000 - expectedCost, profile.Supplies);
    }

    [Fact]
    public void Locked_weapon_cannot_be_upgraded()
    {
        var profile = new Profile();
        profile.AddSupplies(1000);

        var result = WeaponUpgradeService.TryPurchase(profile, WeaponId.Smg, WeaponUpgradeStat.Damage);

        Assert.Equal(WeaponUpgradePurchaseResult.NotOwned, result);
        Assert.Equal(0, profile.GetWeaponUpgradeTier(WeaponId.Smg, WeaponUpgradeStat.Damage));
        Assert.Equal(1000, profile.Supplies);
    }

    [Fact]
    public void Effective_stats_apply_every_track()
    {
        var profile = new Profile();
        profile.SetWeaponUpgradeTier(WeaponId.Pistol, WeaponUpgradeStat.Damage, 5);
        profile.SetWeaponUpgradeTier(WeaponId.Pistol, WeaponUpgradeStat.FireRate, 5);
        profile.SetWeaponUpgradeTier(WeaponId.Pistol, WeaponUpgradeStat.MagazineSize, 5);
        profile.SetWeaponUpgradeTier(WeaponId.Pistol, WeaponUpgradeStat.ReloadSpeed, 5);

        var baseStats = WeaponCatalog.GetStats(WeaponId.Pistol);
        var stats = WeaponUpgradeService.GetEffectiveStats(profile, WeaponId.Pistol);

        Assert.Equal(baseStats.Damage * 1.5f, stats.Damage, 3);
        Assert.Equal(baseStats.FireInterval / 1.4f, stats.FireInterval, 3);
        Assert.Equal(21, stats.MagazineSize);
        Assert.Equal(baseStats.ReloadSeconds / 1.5f, stats.ReloadSeconds, 3);
    }

    [Fact]
    public void Upgrades_survive_death()
    {
        var profile = new Profile();
        profile.AddSupplies(600);
        WeaponUpgradeService.TryPurchase(profile, WeaponId.Pistol, WeaponUpgradeStat.Damage);
        WeaponUpgradeService.TryPurchase(profile, WeaponId.Pistol, WeaponUpgradeStat.Damage);

        var run = new RunState(GameBalance.PlayerMaxHealth);
        run.DamagePlayer(GameBalance.PlayerMaxHealth * 2f);
        profile.RecordRunEnd(run);

        Assert.Equal(2, profile.GetWeaponUpgradeTier(WeaponId.Pistol, WeaponUpgradeStat.Damage));
    }

    [Fact]
    public void Validate_clamps_and_deduplicates_upgrade_entries()
    {
        var profile = new Profile
        {
            WeaponUpgrades = new List<WeaponUpgrade>
            {
                new WeaponUpgrade(WeaponId.Pistol, WeaponUpgradeStat.Damage, 99),
                new WeaponUpgrade(WeaponId.Pistol, WeaponUpgradeStat.Damage, 3),
                new WeaponUpgrade(WeaponId.Smg, WeaponUpgradeStat.Damage, 2),
                new WeaponUpgrade((WeaponId)99, WeaponUpgradeStat.Damage, 1),
                new WeaponUpgrade(WeaponId.Pistol, (WeaponUpgradeStat)99, 1),
                new WeaponUpgrade(WeaponId.Pistol, WeaponUpgradeStat.ReloadSpeed, 0)
            }
        };

        profile.Validate();

        Assert.Single(profile.WeaponUpgrades);
        Assert.Equal(5, profile.GetWeaponUpgradeTier(WeaponId.Pistol, WeaponUpgradeStat.Damage));
    }

    [Fact]
    public void Unknown_weapon_and_stat_are_rejected()
    {
        var profile = new Profile();
        profile.AddSupplies(1000);

        Assert.Equal(
            WeaponUpgradePurchaseResult.UnknownWeapon,
            WeaponUpgradeService.TryPurchase(profile, (WeaponId)99, WeaponUpgradeStat.Damage));
        Assert.Equal(
            WeaponUpgradePurchaseResult.UnknownStat,
            WeaponUpgradeService.TryPurchase(profile, WeaponId.Pistol, (WeaponUpgradeStat)99));
        Assert.Equal(1000, profile.Supplies);
    }

    [Fact]
    public void Next_cost_returns_zero_at_max_tier()
    {
        var profile = new Profile();
        var track = WeaponUpgradeCatalog.Get(WeaponId.Pistol, WeaponUpgradeStat.Damage);

        Assert.Equal(track.GetCost(1), WeaponUpgradeService.GetNextCost(profile, WeaponId.Pistol, WeaponUpgradeStat.Damage));

        profile.SetWeaponUpgradeTier(WeaponId.Pistol, WeaponUpgradeStat.Damage, track.MaxTier);

        Assert.Equal(0, WeaponUpgradeService.GetNextCost(profile, WeaponId.Pistol, WeaponUpgradeStat.Damage));
    }
}
