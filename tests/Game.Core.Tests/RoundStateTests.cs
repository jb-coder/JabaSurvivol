namespace JabaSurvivol.Core.Tests;

public sealed class RoundStateTests
{
    [Fact]
    public void Start_makes_the_round_active_with_its_spawn_budget()
    {
        var round = new RoundState(1);

        Assert.Equal(RoundPhase.NotStarted, round.Phase);
        Assert.Equal(RoundTable.Get(1).SpawnBudget, round.EnemiesRemaining);

        round.Start();

        Assert.True(round.IsActive);
        Assert.Equal(round.SpawnBudget, round.EnemiesRemaining);
        Assert.Equal(0, round.EnemiesSpawned);
    }

    [Fact]
    public void RegisterKill_reduces_remaining_enemies()
    {
        var round = new RoundState(1);
        round.Start();

        var result = round.RegisterKill();

        Assert.True(result.Accepted);
        Assert.False(result.RoundCleared);
        Assert.Equal(round.SpawnBudget - 1, result.EnemiesRemaining);
        Assert.Equal(1, round.KillsThisRound);
    }

    [Fact]
    public void RegisterKill_on_the_last_enemy_clears_the_round()
    {
        var round = new RoundState(1);
        round.Start();

        RoundKillResult result = default;
        for (var i = 0; i < round.SpawnBudget; i++)
        {
            result = round.RegisterKill();
        }

        Assert.True(result.Accepted);
        Assert.True(result.RoundCleared);
        Assert.Equal(RoundPhase.Cleared, round.Phase);
        Assert.Equal(0, round.EnemiesRemaining);
    }

    [Fact]
    public void TryAdvanceToNextRound_requires_a_cleared_round()
    {
        var round = new RoundState(1);
        round.Start();

        Assert.False(round.TryAdvanceToNextRound());
        Assert.Equal(1, round.RoundNumber);
    }

    [Fact]
    public void TryAdvanceToNextRound_resets_budget_and_scaling()
    {
        var round = new RoundState(1);
        round.Start();
        for (var i = 0; i < round.SpawnBudget; i++)
        {
            round.RegisterKill();
        }

        Assert.True(round.TryAdvanceToNextRound());
        Assert.Equal(2, round.RoundNumber);
        Assert.True(round.IsActive);
        Assert.Equal(RoundTable.Get(2).SpawnBudget, round.EnemiesRemaining);
        Assert.Equal(0, round.EnemiesSpawned);
        Assert.Equal(0, round.KillsThisRound);
        Assert.True(round.Definition.ZombieMaxHealth > RoundTable.Get(1).ZombieMaxHealth);
    }

    [Fact]
    public void Fail_marks_an_active_round_failed()
    {
        var round = new RoundState(1);
        round.Start();

        round.Fail();

        Assert.True(round.IsFailed);
        Assert.False(round.IsActive);
        Assert.False(round.TryAdvanceToNextRound());
    }

    [Fact]
    public void Reset_restores_the_starting_round()
    {
        var round = new RoundState(1);
        round.Start();
        for (var i = 0; i < round.SpawnBudget; i++)
        {
            round.RegisterKill();
        }

        round.Reset(1);
        round.Start();

        Assert.Equal(1, round.RoundNumber);
        Assert.True(round.IsActive);
        Assert.Equal(RoundTable.Get(1).SpawnBudget, round.EnemiesRemaining);
        Assert.Equal(0, round.KillsThisRound);
    }

    [Fact]
    public void TryMarkSpawned_never_exceeds_the_spawn_budget()
    {
        var round = new RoundState(1);
        round.Start();

        for (var i = 0; i < round.SpawnBudget; i++)
        {
            Assert.True(round.TryMarkSpawned());
        }

        Assert.False(round.CanSpawnMore);
        Assert.False(round.TryMarkSpawned());
        Assert.Equal(round.SpawnBudget, round.EnemiesSpawned);
    }

    [Fact]
    public void Final_round_cannot_advance()
    {
        var round = new RoundState(GameBalance.MaxRound);
        round.Start();
        for (var i = 0; i < round.SpawnBudget; i++)
        {
            round.RegisterKill();
        }

        Assert.True(round.IsFinalRound);
        Assert.True(round.IsCleared);
        Assert.False(round.TryAdvanceToNextRound());
        Assert.Equal(GameBalance.MaxRound, round.RoundNumber);
    }

    [Fact]
    public void RoundTable_scales_spawn_budget_health_speed_and_damage()
    {
        var first = RoundTable.Get(1);
        var fifth = RoundTable.Get(5);
        var tenth = RoundTable.Get(10);

        Assert.True(fifth.SpawnBudget > first.SpawnBudget);
        Assert.True(tenth.SpawnBudget > fifth.SpawnBudget);
        Assert.True(fifth.ZombieMaxHealth > first.ZombieMaxHealth);
        Assert.True(tenth.ZombieMaxHealth > fifth.ZombieMaxHealth);
        Assert.True(fifth.ZombieMoveSpeed > first.ZombieMoveSpeed);
        Assert.True(tenth.ZombieContactDamage > fifth.ZombieContactDamage);
        Assert.True(fifth.IsElite);
        Assert.True(tenth.IsBoss);
        Assert.False(first.IsElite);
        Assert.False(first.IsBoss);
    }

    [Fact]
    public void RoundTable_caps_zombie_speed_growth()
    {
        var lastRound = RoundTable.Get(GameBalance.MaxRound);

        Assert.Equal(
            GameBalance.ZombieMoveSpeed * GameBalance.RoundZombieSpeedCapMultiplier,
            lastRound.ZombieMoveSpeed,
            precision: 4);
    }

    [Fact]
    public void RoundTable_rejects_invalid_round_numbers()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RoundTable.Get(0));
    }
}
