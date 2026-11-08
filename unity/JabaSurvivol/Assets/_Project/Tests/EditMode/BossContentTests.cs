using JabaSurvivol.Core;
using NUnit.Framework;

namespace JabaSurvivol.Tests.EditMode
{
    public sealed class BossContentTests
    {
        [Test]
        public void Boss_definition_has_a_telegraphed_slam_and_enrage_phase()
        {
            var boss = BossCatalog.BossDefinition;

            Assert.That(boss.MaxHealth, Is.GreaterThan(0f));
            Assert.That(boss.SlamDamage, Is.GreaterThan(boss.ContactDamage));
            Assert.That(boss.SlamTelegraphSeconds, Is.GreaterThan(0f));
            Assert.That(boss.SlamRadius, Is.GreaterThan(0f));
            Assert.That(boss.EnragedSlamIntervalSeconds, Is.LessThan(boss.SlamIntervalSeconds));
            Assert.That(boss.Supplies, Is.EqualTo(1000));
            Assert.That(boss.Xp, Is.EqualTo(500));
        }

        [Test]
        public void Boss_round_waits_for_the_boss_before_clearing()
        {
            var run = new RunState(GameBalance.PlayerMaxHealth, startingRound: GameBalance.BossRound);
            for (var i = 0; i < run.SpawnBudget; i++)
            {
                run.RegisterKill(ZombieArchetype.Walker);
            }

            Assert.That(run.BossSpawnPending, Is.True);
            Assert.That(run.RoundModel.Phase, Is.EqualTo(RoundPhase.Active));
            Assert.That(run.TrySpawnBoss(), Is.True);
            Assert.That(run.BossActive, Is.True);
            Assert.That(run.BossSpawnPending, Is.False);
        }

        [Test]
        public void Boss_kill_grants_the_victory_reward_once()
        {
            var run = new RunState(GameBalance.PlayerMaxHealth, startingRound: GameBalance.BossRound);
            for (var i = 0; i < run.SpawnBudget; i++)
            {
                run.RegisterKill(ZombieArchetype.Walker);
            }

            Assert.That(run.TrySpawnBoss(), Is.True);
            Assert.That(run.ApplyBossDamage(run.Boss.MaxHealth, out var reward), Is.True);
            Assert.That(reward.Supplies, Is.EqualTo(1000));
            Assert.That(reward.Xp, Is.EqualTo(500));
            Assert.That(reward.IsBoss, Is.True);
            Assert.That(run.IsVictory, Is.True);
            Assert.That(run.RoundModel.Phase, Is.EqualTo(RoundPhase.Cleared));

            Assert.That(run.RegisterBossKill().Supplies, Is.EqualTo(0));
        }

        [Test]
        public void Boss_enrages_below_half_health()
        {
            var run = new RunState(GameBalance.PlayerMaxHealth, startingRound: GameBalance.BossRound);
            for (var i = 0; i < run.SpawnBudget; i++)
            {
                run.RegisterKill(ZombieArchetype.Walker);
            }

            run.TrySpawnBoss();
            run.ApplyBossDamage(run.Boss.MaxHealth * 0.6f, out _);

            Assert.That(run.Boss.IsEnraged, Is.True);
            Assert.That(run.Boss.IsAlive, Is.True);
        }
    }
}
