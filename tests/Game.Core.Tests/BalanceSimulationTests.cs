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

        Assert.Equal(first.TotalSupplies, second.TotalSupplies);
        Assert.Equal(first.TotalXp, second.TotalXp);
        Assert.Equal(first.Rounds.Count, second.Rounds.Count);

        for (var i = 0; i < first.Rounds.Count; i++)
        {
            Assert.Equal(first.Rounds[i].Reward.Type, second.Rounds[i].Reward.Type);
            Assert.Equal(first.Rounds[i].SuppliesEarned, second.Rounds[i].SuppliesEarned);
        }
    }

    [Fact]
    public void Run_reports_round_pacing_and_economy_growth()
    {
        var report = BalanceSimulation.Run();

        Assert.True(report.TotalSupplies > 0);
        Assert.True(report.TotalXp > 0);
        Assert.True(report.FinalLevel >= 2);
        Assert.True(report.FinalSkillPoints >= 1);
        Assert.True(report.Rounds[^1].CumulativeSupplies > report.Rounds[0].CumulativeSupplies);
        Assert.True(report.Rounds[^1].ShotsToKill > report.Rounds[0].ShotsToKill);
    }

    [Fact]
    public void Run_reports_no_outliers_for_the_default_balance()
    {
        var report = BalanceSimulation.Run();

        Assert.True(report.Rounds.All(round => round.ShotsToKill > 1));
        Assert.DoesNotContain(report.Outliers, outlier => outlier.Contains("trivial", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(report.Outliers, outlier => outlier.Contains("unreachable", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(report.Outliers, outlier => outlier.Contains("runaway", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Run_flags_unreachable_upgrades_when_the_cost_exceeds_income()
    {
        var report = BalanceSimulation.Run(new BalanceSimulationOptions { UnreachableUpgradeCost = 1_000_000 });

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
        Assert.Throws<ArgumentOutOfRangeException>(
            () => BalanceSimulation.Run(new BalanceSimulationOptions { PistolDamage = 0f }));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => BalanceSimulation.Run(new BalanceSimulationOptions { FireInterval = 0f }));
    }
}
