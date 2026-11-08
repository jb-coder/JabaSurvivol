using System.Linq;
using JabaSurvivol.Core;
using NUnit.Framework;

namespace JabaSurvivol.Tests.EditMode
{
    public sealed class RoundRewardTests
    {
        [Test]
        public void Reward_generation_is_deterministic_and_distinct()
        {
            var first = RoundRewardModel.GenerateOptions(3, 4242);
            var second = RoundRewardModel.GenerateOptions(3, 4242);

            Assert.That(first.Select(option => option.Type), Is.EqualTo(second.Select(option => option.Type)));
            Assert.That(first.Select(option => option.Type).Distinct().Count(), Is.EqualTo(GameBalance.RoundRewardOptionCount));
        }

        [Test]
        public void Persistent_rewards_are_saved_and_run_only_rewards_are_not()
        {
            var profile = new Profile();
            profile.UnlockWeapon(WeaponId.Smg);
            profile.EquipWeapon(WeaponId.Smg);
            var run = new RunState(GameBalance.PlayerMaxHealth, profile: profile);
            var initialReserve = run.Reserve;

            RoundRewardModel.Apply(new RoundRewardOption(RoundRewardType.Supplies, 75), profile, run);
            RoundRewardModel.Apply(new RoundRewardOption(RoundRewardType.Xp, 30), profile, run);
            RoundRewardModel.Apply(
                new RoundRewardOption(RoundRewardType.AmmoCrate, GameBalance.AmmoCrateAmount),
                profile,
                run);

            Assert.That(run.Reserve, Is.EqualTo(initialReserve + GameBalance.AmmoCrateAmount));

            run.DamagePlayer(GameBalance.PlayerMaxHealth * 2f);
            profile.RecordRunEnd(run);
            run.Restart();

            Assert.That(profile.Supplies, Is.EqualTo(75));
            Assert.That(profile.Xp, Is.EqualTo(30));
            Assert.That(run.Reserve, Is.EqualTo(initialReserve));
        }

        [Test]
        public void Reward_amounts_scale_with_the_round()
        {
            var first = RoundRewardModel.GetAmount(RoundRewardType.Supplies, 1);
            var tenth = RoundRewardModel.GetAmount(RoundRewardType.Supplies, 10);

            Assert.That(tenth, Is.GreaterThan(first));
        }
    }
}
