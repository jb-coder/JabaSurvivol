namespace JabaSurvivol.Core.Tests;

public sealed class RunStateTests
{
    [Fact]
    public void RegisterKill_awards_supplies_and_xp()
    {
        var run = new RunState(GameBalance.PlayerMaxHealth);

        var reward = run.RegisterKill();

        Assert.Equal(1, run.Kills);
        Assert.Equal(1, run.Round);
        Assert.Equal(GameBalance.SuppliesPerKill, reward.Supplies);
        Assert.Equal(GameBalance.XpPerKill, reward.Xp);
        Assert.False(reward.CompletedRound);
        Assert.Equal(run.SpawnBudget - 1, reward.EnemiesRemaining);
    }

    [Fact]
    public void RegisterKill_completes_round_and_waits_for_reward_flow()
    {
        var run = new RunState(GameBalance.PlayerMaxHealth);
        KillReward reward = default;

        for (var i = 0; i < run.SpawnBudget; i++)
        {
            reward = run.RegisterKill();
        }

        Assert.True(reward.CompletedRound);
        Assert.Equal(1, reward.Round);
        Assert.Equal(0, reward.EnemiesRemaining);
        Assert.Equal(1, run.Round);
        Assert.Equal(RoundPhase.Cleared, run.RoundModel.Phase);
        Assert.False(run.RoundModel.IsActive);
    }

    [Fact]
    public void AdvanceRound_starts_the_next_round_with_a_larger_budget()
    {
        var run = new RunState(GameBalance.PlayerMaxHealth);
        var firstBudget = run.SpawnBudget;

        for (var i = 0; i < firstBudget; i++)
        {
            run.RegisterKill();
        }

        Assert.True(run.AdvanceRound());
        Assert.Equal(2, run.Round);
        Assert.Equal(RoundPhase.Active, run.RoundModel.Phase);
        Assert.True(run.SpawnBudget > firstBudget);
        Assert.Equal(0, run.KillsThisRound);
        Assert.Equal(run.SpawnBudget, run.EnemiesRemaining);
    }

    [Fact]
    public void RegisterKill_is_ignored_while_round_is_cleared()
    {
        var run = new RunState(GameBalance.PlayerMaxHealth);

        for (var i = 0; i < run.SpawnBudget; i++)
        {
            run.RegisterKill();
        }

        var killsAfterClear = run.Kills;
        var reward = run.RegisterKill();

        Assert.Equal(killsAfterClear, run.Kills);
        Assert.Equal(0, reward.Supplies);
        Assert.Equal(0, reward.Xp);
    }

    [Fact]
    public void AdvanceRound_is_rejected_while_the_round_is_active()
    {
        var run = new RunState(GameBalance.PlayerMaxHealth);

        Assert.False(run.AdvanceRound());
        Assert.Equal(1, run.Round);
    }

    [Fact]
    public void DamagePlayer_ends_run_and_restart_resets_only_the_run()
    {
        var run = new RunState(GameBalance.PlayerMaxHealth);
        run.AddAmmo(4);
        run.AddArmor(2);

        Assert.True(run.DamagePlayer(GameBalance.PlayerMaxHealth + 2f));
        Assert.True(run.IsGameOver);
        Assert.False(run.Player.IsAlive);
        Assert.Equal(0, run.Armor);
        Assert.Equal(RoundPhase.Failed, run.RoundModel.Phase);

        run.Restart();

        Assert.False(run.IsGameOver);
        Assert.True(run.Player.IsAlive);
        Assert.Equal(GameBalance.PlayerMaxHealth, run.Player.Health);
        Assert.Equal(0, run.Kills);
        Assert.Equal(1, run.Round);
        Assert.Equal(0, run.Ammo);
        Assert.Equal(0, run.Armor);
        Assert.Equal(RoundPhase.Active, run.RoundModel.Phase);
    }

    [Fact]
    public void RegisterKill_after_game_over_returns_no_reward()
    {
        var run = new RunState(GameBalance.PlayerMaxHealth);
        run.DamagePlayer(GameBalance.PlayerMaxHealth);

        var reward = run.RegisterKill();

        Assert.Equal(0, reward.Supplies);
        Assert.Equal(0, run.Kills);
    }

    [Fact]
    public void Player_damage_is_clamped_to_remaining_health()
    {
        var player = new PlayerState(100f);

        Assert.Equal(30f, player.ApplyDamage(30f));
        Assert.Equal(70f, player.Health);
        Assert.Equal(70f, player.ApplyDamage(100f));
        Assert.Equal(0f, player.Health);
        Assert.Equal(0f, player.ApplyDamage(10f));
    }
}
