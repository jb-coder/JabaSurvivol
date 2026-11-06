using JabaSurvivol.Core;
using NUnit.Framework;

namespace JabaSurvivol.Tests.EditMode
{
    public sealed class RoundLifecycleTests
    {
        [Test]
        public void Run_starts_the_first_round_with_a_spawn_budget()
        {
            var run = new RunState(GameBalance.PlayerMaxHealth);

            Assert.That(run.Round, Is.EqualTo(1));
            Assert.That(run.RoundModel.IsActive, Is.True);
            Assert.That(run.EnemiesRemaining, Is.EqualTo(run.SpawnBudget));
            Assert.That(run.SpawnBudget, Is.EqualTo(RoundTable.Get(1).SpawnBudget));
        }

        [Test]
        public void Round_clear_waits_for_reward_flow_before_advancing()
        {
            var run = new RunState(GameBalance.PlayerMaxHealth);
            var budget = run.SpawnBudget;

            KillReward reward = default;
            for (var i = 0; i < budget; i++)
            {
                reward = run.RegisterKill();
            }

            Assert.That(reward.CompletedRound, Is.True);
            Assert.That(run.RoundModel.Phase, Is.EqualTo(RoundPhase.Cleared));
            Assert.That(run.Round, Is.EqualTo(1));

            Assert.That(run.AdvanceRound(), Is.True);
            Assert.That(run.Round, Is.EqualTo(2));
            Assert.That(run.RoundModel.Phase, Is.EqualTo(RoundPhase.Active));
            Assert.That(run.EnemiesRemaining, Is.EqualTo(run.SpawnBudget));
        }

        [Test]
        public void Scaling_increases_health_speed_damage_and_budget_by_round()
        {
            var first = RoundTable.Get(1);
            var tenth = RoundTable.Get(GameBalance.MaxRound);

            Assert.That(tenth.SpawnBudget, Is.GreaterThan(first.SpawnBudget));
            Assert.That(tenth.ZombieMaxHealth, Is.GreaterThan(first.ZombieMaxHealth));
            Assert.That(tenth.ZombieMoveSpeed, Is.GreaterThan(first.ZombieMoveSpeed));
            Assert.That(tenth.ZombieContactDamage, Is.GreaterThan(first.ZombieContactDamage));
            Assert.That(tenth.IsBoss, Is.True);
        }

        [Test]
        public void Death_fails_the_round_and_restart_restores_the_first_round()
        {
            var profile = new Profile { Supplies = 300, HighestRound = 4 };
            var run = new RunState(GameBalance.PlayerMaxHealth);
            run.AddAmmo(3);
            run.AddArmor(1);

            run.DamagePlayer(GameBalance.PlayerMaxHealth * 2f);
            profile.RecordRunEnd(run);

            Assert.That(run.IsGameOver, Is.True);
            Assert.That(run.RoundModel.Phase, Is.EqualTo(RoundPhase.Failed));
            Assert.That(profile.Supplies, Is.EqualTo(300));
            Assert.That(profile.HighestRound, Is.EqualTo(4));

            run.Restart();

            Assert.That(run.IsGameOver, Is.False);
            Assert.That(run.Round, Is.EqualTo(1));
            Assert.That(run.RoundModel.Phase, Is.EqualTo(RoundPhase.Active));
            Assert.That(run.Ammo, Is.EqualTo(0));
            Assert.That(run.Armor, Is.EqualTo(0));
        }

        [Test]
        public void Highest_round_record_updates_when_a_run_reaches_a_new_round()
        {
            var profile = new Profile { HighestRound = 1 };
            var run = new RunState(GameBalance.PlayerMaxHealth);

            for (var i = 0; i < run.SpawnBudget; i++)
            {
                run.RegisterKill();
            }

            run.AdvanceRound();
            profile.RecordRunEnd(run);

            Assert.That(profile.HighestRound, Is.EqualTo(2));
        }
    }
}
