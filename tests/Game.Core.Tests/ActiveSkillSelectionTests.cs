namespace JabaSurvivol.Core.Tests;

public sealed class ActiveSkillSelectionTests
{
    [Fact]
    public void Zero_rank_and_passive_nodes_cannot_be_selected()
    {
        var profile = new Profile();

        Assert.False(profile.SetActiveSkill(SkillNode.Dash));
        Assert.False(profile.SetActiveSkill(SkillNode.Strength));
        Assert.Equal(SkillNode.Turret, profile.ActiveSkill);
    }

    [Fact]
    public void Allocated_active_nodes_can_be_selected()
    {
        var profile = new Profile();
        profile.SetSkillRank(SkillNode.Dash, 1);
        profile.SetSkillRank(SkillNode.Shockwave, 1);

        Assert.True(profile.SetActiveSkill(SkillNode.Dash));
        Assert.Equal(SkillNode.Dash, profile.ActiveSkill);
        Assert.True(profile.SetActiveSkill(SkillNode.Shockwave));
        Assert.Equal(SkillNode.Shockwave, profile.ActiveSkill);
    }

    [Fact]
    public void Validation_falls_back_when_the_selected_skill_has_no_rank()
    {
        var profile = new Profile
        {
            Level = 5,
            ActiveSkill = SkillNode.Dash
        };
        profile.SetSkillRank(SkillNode.Dash, 2);
        profile.SetActiveSkill(SkillNode.Dash);
        profile.SetSkillRank(SkillNode.Dash, 0);
        profile.SetSkillRank(SkillNode.Shockwave, 1);

        profile.Validate();

        Assert.Equal(SkillNode.Shockwave, profile.ActiveSkill);
    }

    [Fact]
    public void Dash_scales_distance_and_cooldown()
    {
        var dash = SkillCatalog.Get(SkillNode.Dash).GetActivationStats(1);

        Assert.Equal(3f, dash.DashDistance, precision: 3);
        Assert.Equal(5.5f, dash.CooldownSeconds, precision: 3);
    }

    [Fact]
    public void Dash_activation_starts_invulnerability_and_cooldown()
    {
        var profile = new Profile();
        profile.SetSkillRank(SkillNode.Dash, 1);
        profile.SetActiveSkill(SkillNode.Dash);
        var run = new RunState(GameBalance.PlayerMaxHealth, profile: profile);

        Assert.True(run.TryActivateActiveSkill());
        Assert.True(run.ActiveSkill.IsActive);

        run.TickActiveSkill(GameBalance.DashInvulnerabilitySeconds + 0.01f);

        Assert.False(run.ActiveSkill.IsActive);
        Assert.True(run.ActiveSkill.CooldownRemaining > 0f);
    }

    [Fact]
    public void Shockwave_scales_damage_radius_and_cooldown()
    {
        var shockwave = SkillCatalog.Get(SkillNode.Shockwave).GetActivationStats(1);

        Assert.Equal(40f, shockwave.AreaDamage, precision: 3);
        Assert.Equal(2f, shockwave.AreaRadius, precision: 3);
        Assert.Equal(9f, shockwave.CooldownSeconds, precision: 3);
    }

    [Fact]
    public void Shockwave_resolves_instantly_and_only_starts_cooldown()
    {
        var profile = new Profile();
        profile.SetSkillRank(SkillNode.Shockwave, 1);
        profile.SetActiveSkill(SkillNode.Shockwave);
        var run = new RunState(GameBalance.PlayerMaxHealth, profile: profile);

        Assert.True(run.TryActivateActiveSkill());
        Assert.False(run.ActiveSkill.IsActive);
        Assert.True(run.ActiveSkill.CooldownRemaining > 0f);
        Assert.False(run.TryActivateActiveSkill());
    }

    [Fact]
    public void Selected_active_skill_survives_death_and_resets_run_state()
    {
        var profile = new Profile();
        profile.SetSkillRank(SkillNode.Shockwave, 2);
        profile.SetActiveSkill(SkillNode.Shockwave);
        var run = new RunState(GameBalance.PlayerMaxHealth, profile: profile);
        run.TryActivateActiveSkill();
        run.TickActiveSkill(0.1f);

        run.DamagePlayer(GameBalance.PlayerMaxHealth * 2f);

        Assert.True(run.IsGameOver);
        Assert.False(run.ActiveSkill.IsActive);
        Assert.Equal(0f, run.ActiveSkill.CooldownRemaining);
        Assert.Equal(SkillNode.Shockwave, profile.ActiveSkill);
    }
}
