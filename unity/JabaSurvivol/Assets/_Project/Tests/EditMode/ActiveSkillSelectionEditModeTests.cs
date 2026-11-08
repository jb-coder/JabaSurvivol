using JabaSurvivol.Core;
using NUnit.Framework;

namespace JabaSurvivol.Tests.EditMode
{
    public sealed class ActiveSkillSelectionEditModeTests
    {
        [Test]
        public void Selection_requires_an_allocated_active_rank()
        {
            var profile = new Profile();

            Assert.That(profile.SetActiveSkill(SkillNode.Dash), Is.False);
            Assert.That(profile.SetActiveSkill(SkillNode.Strength), Is.False);

            profile.SetSkillRank(SkillNode.Dash, 1);
            Assert.That(profile.SetActiveSkill(SkillNode.Dash), Is.True);
            Assert.That(profile.ActiveSkill, Is.EqualTo(SkillNode.Dash));
        }

        [Test]
        public void Selected_active_skill_persists_through_validation()
        {
            var profile = new Profile { Level = 5 };
            profile.SetSkillRank(SkillNode.Shockwave, 1);
            profile.SetActiveSkill(SkillNode.Shockwave);
            profile.Validate();

            Assert.That(profile.ActiveSkill, Is.EqualTo(SkillNode.Shockwave));
        }

        [Test]
        public void Dash_and_shockwave_expose_rank_scaled_values()
        {
            var dash = SkillCatalog.Get(SkillNode.Dash).GetActivationStats(3);
            var shockwave = SkillCatalog.Get(SkillNode.Shockwave).GetActivationStats(3);

            Assert.That(dash.DashDistance, Is.EqualTo(4f));
            Assert.That(dash.CooldownSeconds, Is.EqualTo(4.5f));
            Assert.That(shockwave.AreaDamage, Is.EqualTo(60f));
            Assert.That(shockwave.AreaRadius, Is.EqualTo(2.5f));
            Assert.That(shockwave.CooldownSeconds, Is.EqualTo(7f));
        }
    }
}
