using JabaSurvivol.Core;
using NUnit.Framework;

namespace JabaSurvivol.Tests.EditMode
{
    public sealed class CoreProgressionTests
    {
        [Test]
        public void Death_does_not_remove_persistent_progress()
        {
            var profile = new Profile { Supplies = 500, Level = 6, SkillPoints = 7 };
            var run = new RunState(GameBalance.PlayerMaxHealth);

            run.RegisterKill();
            profile.AddSupplies(GameBalance.SuppliesPerKill);
            profile.AddXp(GameBalance.XpPerKill);

            Assert.That(run.DamagePlayer(1000f), Is.True);
            profile.RecordRunEnd(run);

            Assert.That(profile.Supplies, Is.EqualTo(510));
            Assert.That(profile.Level, Is.GreaterThanOrEqualTo(6));
            Assert.That(profile.SkillPoints, Is.GreaterThanOrEqualTo(7));
        }

        [Test]
        public void Restart_resets_run_state_only()
        {
            var profile = new Profile { Supplies = 250 };
            var run = new RunState(GameBalance.PlayerMaxHealth);
            run.RegisterKill();
            run.DamagePlayer(1000f);

            run.Restart();

            Assert.That(run.IsGameOver, Is.False);
            Assert.That(run.Player.Health, Is.EqualTo(GameBalance.PlayerMaxHealth));
            Assert.That(run.Kills, Is.EqualTo(0));
            Assert.That(run.Round, Is.EqualTo(1));
            Assert.That(profile.Supplies, Is.EqualTo(250));
        }
    }
}
