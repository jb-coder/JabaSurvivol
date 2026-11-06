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
    }

    [Fact]
    public void RegisterKill_completes_round_and_increases_round()
    {
        var run = new RunState(GameBalance.PlayerMaxHealth);
        KillReward reward = default;

        for (var i = 0; i < GameBalance.KillsPerRound; i++)
        {
            reward = run.RegisterKill();
        }

        Assert.True(reward.CompletedRound);
        Assert.Equal(2, reward.Round);
        Assert.Equal(2, run.Round);
        Assert.Equal(0, run.KillsThisRound);
        Assert.Equal(
            GameBalance.SuppliesPerKill + GameBalance.SuppliesPerRoundClear,
            reward.Supplies);
        Assert.Equal(GameBalance.XpPerKill + GameBalance.XpPerRoundClear, reward.Xp);
    }

    [Fact]
    public void DamagePlayer_ends_run_and_restart_resets_only_the_run()
    {
        var run = new RunState(GameBalance.PlayerMaxHealth);

        Assert.True(run.DamagePlayer(GameBalance.PlayerMaxHealth));
        Assert.True(run.IsGameOver);
        Assert.False(run.Player.IsAlive);

        run.Restart();

        Assert.False(run.IsGameOver);
        Assert.True(run.Player.IsAlive);
        Assert.Equal(GameBalance.PlayerMaxHealth, run.Player.Health);
        Assert.Equal(0, run.Kills);
        Assert.Equal(1, run.Round);
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
