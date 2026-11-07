namespace JabaSurvivol.Core.Tests;

public sealed class BossTests
{
    [Fact]
    public void Boss_catalog_exposes_the_round_ten_encounter()
    {
        var boss = BossCatalog.BossDefinition;

        Assert.True(boss.MaxHealth > RoundTable.Get(GameBalance.BossRound).ZombieMaxHealth);
        Assert.True(boss.SlamDamage > boss.ContactDamage);
        Assert.True(boss.SlamTelegraphSeconds > 0f);
        Assert.Equal(1000, boss.Supplies);
        Assert.Equal(500, boss.Xp);
        Assert.True(boss.EnragedSlamIntervalSeconds < boss.SlamIntervalSeconds);
    }

    [Fact]
    public void Boss_state_enrages_at_half_health()
    {
        var boss = new BossState(BossCatalog.BossDefinition);

        Assert.False(boss.IsEnraged);
        boss.ApplyDamage(boss.MaxHealth * 0.6f);
        Assert.True(boss.IsEnraged);
        Assert.True(boss.IsAlive);
        Assert.True(boss.ApplyDamage(boss.MaxHealth));
        Assert.False(boss.IsAlive);
    }

    [Fact]
    public void Boss_round_does_not_clear_until_the_boss_dies()
    {
        var run = new RunState(GameBalance.PlayerMaxHealth, startingRound: GameBalance.BossRound);

        KillReward lastRegular = default;
        for (var i = 0; i < run.SpawnBudget; i++)
        {
            lastRegular = run.RegisterKill(ZombieArchetype.Walker);
        }

        Assert.False(lastRegular.CompletedRound);
        Assert.Equal(RoundPhase.Active, run.RoundModel.Phase);
        Assert.True(run.BossSpawnPending);
        Assert.False(run.BossActive);
        Assert.True(run.TrySpawnBoss());
        Assert.True(run.BossActive);
        Assert.False(run.BossSpawnPending);
        Assert.False(run.TrySpawnBoss());
    }

    [Fact]
    public void Boss_spawn_is_only_available_in_the_boss_round()
    {
        var run = new RunState(GameBalance.PlayerMaxHealth);

        Assert.False(run.TrySpawnBoss());
        Assert.False(run.BossActive);
    }

    [Fact]
    public void Boss_kill_awards_the_configured_reward_once()
    {
        var run = CreateBossRun();

        Assert.False(run.ApplyBossDamage(10f, out _));
        Assert.True(run.ApplyBossDamage(run.Boss!.MaxHealth, out var reward));

        Assert.True(reward.CompletedRound);
        Assert.True(reward.IsBoss);
        Assert.Equal(1000, reward.Supplies);
        Assert.Equal(500, reward.Xp);
        Assert.True(run.IsVictory);
        Assert.True(run.BossDefeated);
        Assert.False(run.BossActive);
        Assert.Equal(RoundPhase.Cleared, run.RoundModel.Phase);

        var duplicate = run.RegisterBossKill();
        Assert.Equal(0, duplicate.Supplies);
        Assert.Equal(0, duplicate.Xp);
    }

    [Fact]
    public void Restart_resets_boss_and_victory_state()
    {
        var run = CreateBossRun();
        run.ApplyBossDamage(run.Boss!.MaxHealth, out _);
        Assert.True(run.IsVictory);

        run.Restart();

        Assert.False(run.IsVictory);
        Assert.False(run.BossActive);
        Assert.False(run.BossDefeated);
        Assert.False(run.BossSpawnPending);
        Assert.Equal(GameBalance.BossRound, run.Round);
        Assert.Equal(RoundPhase.Active, run.RoundModel.Phase);
    }

    [Fact]
    public void Death_during_the_boss_round_ends_the_run_without_a_victory()
    {
        var run = CreateBossRun();

        Assert.True(run.DamagePlayer(GameBalance.PlayerMaxHealth * 5f));
        Assert.True(run.IsGameOver);
        Assert.False(run.IsVictory);
        Assert.Equal(RoundPhase.Failed, run.RoundModel.Phase);
    }

    private static RunState CreateBossRun()
    {
        var run = new RunState(GameBalance.PlayerMaxHealth, startingRound: GameBalance.BossRound);
        for (var i = 0; i < run.SpawnBudget; i++)
        {
            run.RegisterKill(ZombieArchetype.Walker);
        }

        Assert.True(run.TrySpawnBoss());
        return run;
    }
}
