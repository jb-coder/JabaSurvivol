namespace JabaSurvivol.Core.Tests;

public sealed class BalanceSimulationTests
{
    [Fact]
    public void Run_covers_rounds_1_to_10()
    {
        var report = BalanceSimulation.Run();

        Assert.Equal(GameBalance.MaxRound, report.Rounds.Count);
        Assert.Equal(1, report.Rounds[0].Round);
        Assert.Equal(GameBalance.MaxRound, report.Rounds[^1].Round);
    }

    [Fact]
    public void Run_is_reproducible_for_a_seed()
    {
        var first = BalanceSimulation.Run(new BalanceSimulationOptions { Seed = 4242 });
        var second = BalanceSimulation.Run(new BalanceSimulationOptions { Seed = 4242 });

        Assert.Equal(first.TotalSuppliesEarned, second.TotalSuppliesEarned);
        Assert.Equal(first.TotalSuppliesSpent, second.TotalSuppliesSpent);
        Assert.Equal(first.TotalXp, second.TotalXp);
        Assert.Equal(first.FinalLevel, second.FinalLevel);
        Assert.Equal(first.FinalSkillPoints, second.FinalSkillPoints);
        Assert.Equal(first.Outliers.Count, second.Outliers.Count);
        Assert.Equal(first.Rounds.Count, second.Rounds.Count);

        for (var i = 0; i < first.Rounds.Count; i++)
        {
            Assert.Equal(first.Rounds[i].Reward.Type, second.Rounds[i].Reward.Type);
            Assert.Equal(first.Rounds[i].SuppliesEarned, second.Rounds[i].SuppliesEarned);
            Assert.Equal(first.Rounds[i].XpEarned, second.Rounds[i].XpEarned);
            Assert.Equal(first.Rounds[i].Level, second.Rounds[i].Level);
            Assert.Equal(first.Rounds[i].SkillPoints, second.Rounds[i].SkillPoints);
            Assert.Equal(first.Rounds[i].Build.Weapon, second.Rounds[i].Build.Weapon);
            Assert.Equal(first.Rounds[i].Build.Armor, second.Rounds[i].Build.Armor);
            Assert.Equal(first.Rounds[i].Build.DamageTier, second.Rounds[i].Build.DamageTier);
            Assert.Equal(first.Rounds[i].Build.FireRateTier, second.Rounds[i].Build.FireRateTier);
            Assert.Equal(first.Rounds[i].Build.MagazineTier, second.Rounds[i].Build.MagazineTier);
            Assert.Equal(first.Rounds[i].Build.ReloadTier, second.Rounds[i].Build.ReloadTier);
            Assert.Equal(first.Rounds[i].Build.StrengthRank, second.Rounds[i].Build.StrengthRank);
            Assert.Equal(first.Rounds[i].Build.VitalityRank, second.Rounds[i].Build.VitalityRank);
        }
    }

    [Fact]
    public void Run_reports_round_pacing_economy_and_build_progress()
    {
        var report = BalanceSimulation.Run();
        var last = report.Rounds[^1];

        Assert.True(report.TotalSuppliesEarned > 0);
        Assert.True(report.TotalSuppliesSpent > 0);
        Assert.True(report.TotalXp > 0);
        Assert.True(report.FinalLevel >= 2);
        Assert.True(last.CumulativeXp > report.Rounds[0].CumulativeXp);
        Assert.Equal(WeaponId.Rifle, last.Build.Weapon);
        Assert.True(last.Build.HasRifle);
        Assert.False(last.Build.HasSmg);
        Assert.False(last.Build.HasShotgun);
        Assert.False(last.Build.HasLmg);
        Assert.True(last.Build.DamageTier > 0);
        Assert.True(last.Build.FireRateTier > 0);
        Assert.Equal(ArmorTier.Medium, last.Build.Armor);
        Assert.True(last.Build.TotalArmor >= last.Build.ArmorTierAbsorb);
        Assert.True(last.Build.StrengthRank > 0);
        Assert.True(last.Build.VitalityRank > 0);
    }

    [Fact]
    public void Run_keeps_every_build_inside_the_documented_caps()
    {
        var report = BalanceSimulation.Run();

        Assert.All(report.Rounds, round =>
        {
            var stats = round.Build.Stats;
            Assert.True(stats.WeaponDamageMultiplier <= 1f + StatCaps.DamageBonus + 0.0001f);
            Assert.True(stats.MoveSpeed <= GameBalance.PlayerMoveSpeed * (1f + StatCaps.MoveSpeedBonus) + 0.0001f);
            Assert.True(stats.Armor <= StatCaps.Armor);
            Assert.True(stats.CriticalChance <= StatCaps.CriticalChance + 0.0001f);
            Assert.True(stats.HeadshotBonus <= StatCaps.HeadshotBonus + 0.0001f);
        });
    }

    [Fact]
    public void Run_detects_over_cap_raw_builds()
    {
        var profile = new Profile();
        var weapon = WeaponCatalog.GetStats(WeaponId.Pistol);
        var armor = new ArmorStats(ArmorTier.None, 0, 0, 0, 0, 0, 0);
        var temporary = new TemporaryStatModifiers
        {
            MoveSpeedBonus = 10f,
            DamageBonus = 10f,
            ArmorBonus = 1000,
            CriticalChanceBonus = 10f,
            HeadshotBonusBonus = 10f
        };

        var raw = StatAggregation.ComputeRaw(profile, weapon, armor, temporary);
        var capped = StatAggregation.Compute(profile, weapon, armor, temporary);

        Assert.True(BalanceSimulation.HasCapViolation(raw, out var reason));
        Assert.False(string.IsNullOrWhiteSpace(reason));
        Assert.False(BalanceSimulation.HasCapViolation(capped, out _));
    }

    [Fact]
    public void Run_reports_no_outliers_for_the_default_balance()
    {
        var report = BalanceSimulation.Run();

        Assert.True(report.Rounds.All(round => round.ShotsToKill > 1));
        Assert.DoesNotContain(report.Outliers, outlier => outlier.Contains("trivial", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(report.Outliers, outlier => outlier.Contains("unreachable", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(report.Outliers, outlier => outlier.Contains("runaway", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(report.Outliers, outlier => outlier.Contains("cap", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Run_flags_unreachable_upgrades_when_income_cannot_cover_the_maximum_cost()
    {
        var report = BalanceSimulation.Run(new BalanceSimulationOptions { MaxRound = 1 });

        Assert.Contains(report.Outliers, outlier => outlier.Contains("unreachable", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Run_flags_runaway_supplies_when_the_threshold_is_low()
    {
        var report = BalanceSimulation.Run(new BalanceSimulationOptions { RunawaySupplyThreshold = 1 });

        Assert.Contains(report.Outliers, outlier => outlier.Contains("runaway", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Run_rejects_invalid_options()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => BalanceSimulation.Run(new BalanceSimulationOptions { StartingRound = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => BalanceSimulation.Run(new BalanceSimulationOptions { StartingRound = 5, MaxRound = 4 }));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => BalanceSimulation.Run(new BalanceSimulationOptions { MaxRound = GameBalance.MaxRound + 1 }));
    }
}
