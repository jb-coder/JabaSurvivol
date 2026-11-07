using System.Linq;
using JabaSurvivol.Core;
using NUnit.Framework;

namespace JabaSurvivol.Tests.EditMode
{
    public sealed class ZombieContentTests
    {
        [Test]
        public void Catalog_exposes_three_archetypes()
        {
            Assert.That(ZombieCatalog.All.Count, Is.EqualTo(3));
            Assert.That(
                ZombieCatalog.Get(ZombieArchetype.Runner).Supplies,
                Is.GreaterThan(ZombieCatalog.Get(ZombieArchetype.Walker).Supplies));
            Assert.That(
                ZombieCatalog.Get(ZombieArchetype.Brute).HealthMultiplier,
                Is.GreaterThan(ZombieCatalog.Get(ZombieArchetype.Walker).HealthMultiplier));
        }

        [Test]
        public void Spawn_plan_is_deterministic_and_mixes_archetypes()
        {
            var first = ZombieSpawnPlan.Build(5, 14, 20261107);
            var second = ZombieSpawnPlan.Build(5, 14, 20261107);

            Assert.That(first, Is.EqualTo(second));
            Assert.That(first.Distinct().Count(), Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void Elite_round_rewards_are_multiplied()
        {
            var run = new RunState(GameBalance.PlayerMaxHealth, startingRound: 5);
            var reward = run.RegisterKill(ZombieArchetype.Brute);

            Assert.That(reward.Supplies, Is.EqualTo(60));
            Assert.That(reward.Xp, Is.EqualTo(60));
        }

        [Test]
        public void Round_state_reports_the_archetype_for_each_spawn()
        {
            var round = new RoundState(4, seed: 99);
            round.Start();

            Assert.That(round.TryMarkSpawned(out var archetype), Is.True);
            Assert.That(archetype, Is.EqualTo(round.SpawnPlan[0]));
            Assert.That(round.TryMarkSpawned(out var second), Is.True);
            Assert.That(second, Is.EqualTo(round.SpawnPlan[1]));
        }
    }
}
