namespace JabaSurvivol.Core.Tests;

public sealed class ZombieCatalogTests
{
    [Fact]
    public void Catalog_defines_walker_runner_and_brute_rewards()
    {
        Assert.Equal(3, ZombieCatalog.All.Count);
        Assert.Equal(10, ZombieCatalog.Get(ZombieArchetype.Walker).Supplies);
        Assert.Equal(15, ZombieCatalog.Get(ZombieArchetype.Runner).Supplies);
        Assert.Equal(30, ZombieCatalog.Get(ZombieArchetype.Brute).Supplies);
        Assert.True(
            ZombieCatalog.Get(ZombieArchetype.Runner).SpeedMultiplier >
            ZombieCatalog.Get(ZombieArchetype.Walker).SpeedMultiplier);
        Assert.True(
            ZombieCatalog.Get(ZombieArchetype.Brute).HealthMultiplier >
            ZombieCatalog.Get(ZombieArchetype.Walker).HealthMultiplier);
        Assert.True(
            ZombieCatalog.Get(ZombieArchetype.Brute).VisualScale >
            ZombieCatalog.Get(ZombieArchetype.Walker).VisualScale);
    }

    [Fact]
    public void Spawn_plan_is_deterministic_for_the_same_seed_and_round()
    {
        var first = ZombieSpawnPlan.Build(4, 12, 20261106);
        var second = ZombieSpawnPlan.Build(4, 12, 20261106);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Round_one_only_spawns_walkers()
    {
        var plan = ZombieSpawnPlan.Build(1, RoundTable.Get(1).SpawnBudget, 7);

        Assert.All(plan, archetype => Assert.Equal(ZombieArchetype.Walker, archetype));
    }

    [Fact]
    public void Runner_and_brute_appear_at_their_unlock_rounds()
    {
        var roundTwo = ZombieSpawnPlan.Build(
            GameBalance.RunnerSpawnRound,
            RoundTable.Get(GameBalance.RunnerSpawnRound).SpawnBudget,
            11);
        var roundFour = ZombieSpawnPlan.Build(
            GameBalance.BruteSpawnRound,
            RoundTable.Get(GameBalance.BruteSpawnRound).SpawnBudget,
            11);

        Assert.Contains(ZombieArchetype.Runner, roundTwo);
        Assert.Contains(ZombieArchetype.Runner, roundFour);
        Assert.Contains(ZombieArchetype.Brute, roundFour);
    }

    [Fact]
    public void Spawn_plan_respects_the_spawn_budget()
    {
        var budget = RoundTable.Get(5).SpawnBudget;
        var plan = ZombieSpawnPlan.Build(5, budget, 42);

        Assert.Equal(budget, plan.Length);
    }

    [Fact]
    public void Elite_round_scales_health_damage_and_rewards()
    {
        var eliteRound = RoundTable.Get(5);
        var elite = eliteRound.GetEncounter(ZombieArchetype.Walker);

        Assert.True(eliteRound.IsElite);
        Assert.True(elite.IsElite);
        Assert.Equal(
            eliteRound.ZombieMaxHealth * GameBalance.EliteHealthMultiplier,
            elite.MaxHealth,
            precision: 3);
        Assert.True(elite.ContactDamage > eliteRound.ZombieContactDamage);
        Assert.True(elite.Supplies > ZombieCatalog.Get(ZombieArchetype.Walker).Supplies);
        Assert.True(elite.Xp > ZombieCatalog.Get(ZombieArchetype.Walker).Xp);
    }

    [Fact]
    public void Brute_rewards_are_greater_than_walker_rewards()
    {
        var walker = RoundTable.Get(1).GetEncounter(ZombieArchetype.Walker);
        var brute = RoundTable.Get(1).GetEncounter(ZombieArchetype.Brute);

        Assert.True(brute.MaxHealth > walker.MaxHealth);
        Assert.True(brute.ContactDamage > walker.ContactDamage);
        Assert.True(brute.Supplies > walker.Supplies);
        Assert.True(brute.Xp > walker.Xp);
        Assert.True(brute.Radius > walker.Radius);
    }

    [Fact]
    public void Run_kill_rewards_use_the_archetype_values()
    {
        var run = new RunState(GameBalance.PlayerMaxHealth);

        var walker = run.RegisterKill(ZombieArchetype.Walker);
        var runner = run.RegisterKill(ZombieArchetype.Runner);
        var brute = run.RegisterKill(ZombieArchetype.Brute);

        Assert.Equal(10, walker.Supplies);
        Assert.Equal(15, runner.Supplies);
        Assert.Equal(30, brute.Supplies);
        Assert.Equal(ZombieArchetype.Brute, run.LastKillArchetype);
    }

    [Fact]
    public void Elite_round_kill_rewards_are_multiplied()
    {
        var run = new RunState(GameBalance.PlayerMaxHealth, startingRound: 5);

        var reward = run.RegisterKill(ZombieArchetype.Walker);

        Assert.Equal(20, reward.Supplies);
        Assert.Equal(20, reward.Xp);
    }

    [Fact]
    public void Round_state_spawns_follow_the_generated_plan()
    {
        var round = new RoundState(4, seed: 20261106);
        round.Start();

        for (var i = 0; i < round.SpawnBudget; i++)
        {
            Assert.True(round.TryMarkSpawned(out var archetype));
            Assert.Equal(round.SpawnPlan[i], archetype);
        }

        Assert.False(round.TryMarkSpawned(out _));
    }
}
