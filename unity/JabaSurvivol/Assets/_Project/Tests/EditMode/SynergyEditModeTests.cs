using JabaSurvivol.Core;
using NUnit.Framework;

namespace JabaSurvivol.Tests.EditMode
{
    public sealed class SynergyEditModeTests
    {
        [Test]
        public void Synergy_requires_both_prerequisite_ranks()
        {
            var profile = new Profile();
            profile.SetSkillRank(SkillNode.Strength, 3);
            profile.SetSkillRank(SkillNode.Vitality, 2);

            Assert.That(SynergyCatalog.IsUnlocked(profile, SynergyId.Berserker), Is.False);

            profile.SetSkillRank(SkillNode.Vitality, 3);
            Assert.That(SynergyCatalog.IsUnlocked(profile, SynergyId.Berserker), Is.True);
        }

        [Test]
        public void Ghost_timer_and_bastion_regen_are_run_only()
        {
            var profile = new Profile();
            profile.SetSkillRank(SkillNode.Speed, 3);
            profile.SetSkillRank(SkillNode.Precision, 3);
            profile.SetSkillRank(SkillNode.Endurance, 3);
            profile.SetSkillRank(SkillNode.Engineering, 3);
            var run = new RunState(GameBalance.PlayerMaxHealth, maxArmor: 40, profile: profile);

            run.Synergy.OnKill(profile);
            Assert.That(run.Synergy.GhostActive, Is.True);

            run.DamagePlayer(10f);
            Assert.That(run.TickSynergies(5f), Is.EqualTo(10));
            Assert.That(run.Armor, Is.EqualTo(40));

            run.Restart();

            Assert.That(run.Synergy.GhostActive, Is.False);
            Assert.That(run.Synergy.OutOfCombatSeconds, Is.EqualTo(0f));
        }
    }
}
