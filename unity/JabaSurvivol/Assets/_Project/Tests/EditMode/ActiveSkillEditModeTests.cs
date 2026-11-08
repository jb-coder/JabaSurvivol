using JabaSurvivol.Core;
using NUnit.Framework;

namespace JabaSurvivol.Tests.EditMode
{
    public sealed class ActiveSkillEditModeTests
    {
        [Test]
        public void Turret_requires_an_allocated_rank()
        {
            var run = new RunState(GameBalance.PlayerMaxHealth);

            Assert.That(run.ActiveSkill.Rank, Is.EqualTo(0));
            Assert.That(run.TryActivateActiveSkill(), Is.False);
            Assert.That(run.ActiveSkill.IsActive, Is.False);
        }

        [Test]
        public void Turret_deployment_uses_rank_scaled_values()
        {
            var profile = new Profile();
            profile.SetSkillRank(SkillNode.Turret, 3);
            var run = new RunState(GameBalance.PlayerMaxHealth, profile: profile);

            Assert.That(run.TryActivateActiveSkill(), Is.True);
            Assert.That(run.ActiveSkill.Stats.TurretDamage, Is.EqualTo(12f));
            Assert.That(run.ActiveSkill.Stats.TurretDuration, Is.EqualTo(12f));
            Assert.That(run.ActiveSkill.CooldownRemaining, Is.EqualTo(17f));

            Assert.That(run.ActiveSkill.TryConsumeShot(out var damage), Is.True);
            Assert.That(damage, Is.EqualTo(12f));
        }

        [Test]
        public void Turret_expires_and_cooldown_gates_redeployment()
        {
            var profile = new Profile();
            profile.SetSkillRank(SkillNode.Turret, 1);
            var run = new RunState(GameBalance.PlayerMaxHealth, profile: profile);
            run.TryActivateActiveSkill();

            run.TickActiveSkill(run.ActiveSkill.Stats.TurretDuration + 0.1f);

            Assert.That(run.ActiveSkill.IsActive, Is.False);
            Assert.That(run.TryActivateActiveSkill(), Is.False);

            run.TickActiveSkill(run.ActiveSkill.Stats.CooldownSeconds);
            Assert.That(run.TryActivateActiveSkill(), Is.True);
        }

        [Test]
        public void Restart_resets_turret_state()
        {
            var profile = new Profile();
            profile.SetSkillRank(SkillNode.Turret, 2);
            var run = new RunState(GameBalance.PlayerMaxHealth, profile: profile);
            run.TryActivateActiveSkill();
            run.TickActiveSkill(1f);

            run.Restart();

            Assert.That(run.ActiveSkill.IsActive, Is.False);
            Assert.That(run.ActiveSkill.CooldownRemaining, Is.EqualTo(0f));
            Assert.That(run.ActiveSkill.ActiveRemaining, Is.EqualTo(0f));
        }
    }
}
