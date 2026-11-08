namespace JabaSurvivol.Core.Tests;

public sealed class ActiveSkillTests
{
    [Fact]
    public void Zero_rank_turret_cannot_activate()
    {
        var turret = new ActiveSkillRuntime(SkillNode.Turret, 0);

        Assert.False(turret.CanActivate);
        Assert.False(turret.TryActivate());
        Assert.False(turret.IsActive);
    }

    [Fact]
    public void Turret_rank_scales_damage_duration_and_cooldown()
    {
        var rankOne = SkillCatalog.Get(SkillNode.Turret).GetActivationStats(1);
        var rankFive = SkillCatalog.Get(SkillNode.Turret).GetActivationStats(5);

        Assert.Equal(8f, rankOne.TurretDamage, precision: 3);
        Assert.Equal(8f, rankOne.TurretDuration, precision: 3);
        Assert.Equal(19f, rankOne.CooldownSeconds, precision: 3);
        Assert.Equal(16f, rankFive.TurretDamage, precision: 3);
        Assert.Equal(16f, rankFive.TurretDuration, precision: 3);
        Assert.Equal(15f, rankFive.CooldownSeconds, precision: 3);
    }

    [Fact]
    public void Deploying_a_turret_starts_duration_and_cooldown()
    {
        var turret = new ActiveSkillRuntime(SkillNode.Turret, 1);

        Assert.True(turret.TryActivate());
        Assert.True(turret.IsActive);
        Assert.Equal(turret.Stats.TurretDuration, turret.ActiveRemaining, precision: 3);
        Assert.Equal(turret.Stats.CooldownSeconds, turret.CooldownRemaining, precision: 3);
        Assert.False(turret.TryActivate());
    }

    [Fact]
    public void Turret_fires_on_its_interval()
    {
        var turret = new ActiveSkillRuntime(SkillNode.Turret, 2);
        turret.TryActivate();

        Assert.True(turret.TryConsumeShot(out var first));
        Assert.Equal(turret.Stats.TurretDamage, first, precision: 3);
        Assert.False(turret.TryConsumeShot(out _));

        turret.Tick(GameBalance.TurretFireInterval);
        Assert.True(turret.TryConsumeShot(out var second));
        Assert.Equal(first, second, precision: 3);
    }

    [Fact]
    public void Turret_expires_and_enters_cooldown()
    {
        var turret = new ActiveSkillRuntime(SkillNode.Turret, 1);
        turret.TryActivate();

        turret.Tick(turret.Stats.TurretDuration + 0.01f);

        Assert.False(turret.IsActive);
        Assert.False(turret.CanActivate);
        Assert.True(turret.CooldownRemaining > 0f);

        turret.Tick(turret.Stats.CooldownSeconds);

        Assert.True(turret.CanActivate);
        Assert.True(turret.TryActivate());
    }

    [Fact]
    public void Run_state_uses_the_allocated_turret_rank()
    {
        var profile = new Profile();
        profile.SetSkillRank(SkillNode.Turret, 3);
        var run = new RunState(GameBalance.PlayerMaxHealth, profile: profile);

        Assert.Equal(SkillNode.Turret, run.ActiveSkill.Node);
        Assert.Equal(3, run.ActiveSkill.Rank);
        Assert.True(run.TryActivateActiveSkill());
        Assert.True(run.ActiveSkill.IsActive);
    }

    [Fact]
    public void Refresh_active_skill_picks_up_new_rank_and_node()
    {
        var profile = new Profile();
        profile.SetSkillRank(SkillNode.Turret, 1);
        var run = new RunState(GameBalance.PlayerMaxHealth, profile: profile);

        profile.SetSkillRank(SkillNode.Turret, 4);
        run.RefreshActiveSkill(profile);
        Assert.Equal(4, run.ActiveSkill.Rank);

        profile.ActiveSkill = SkillNode.Dash;
        profile.SetSkillRank(SkillNode.Dash, 2);
        run.RefreshActiveSkill(profile);
        Assert.Equal(SkillNode.Dash, run.ActiveSkill.Node);
        Assert.Equal(2, run.ActiveSkill.Rank);
    }

    [Fact]
    public void Restart_resets_turret_state()
    {
        var profile = new Profile();
        profile.SetSkillRank(SkillNode.Turret, 1);
        var run = new RunState(GameBalance.PlayerMaxHealth, profile: profile);
        run.TryActivateActiveSkill();
        run.TickActiveSkill(1f);

        run.Restart();

        Assert.False(run.ActiveSkill.IsActive);
        Assert.Equal(0f, run.ActiveSkill.CooldownRemaining);
        Assert.Equal(0f, run.ActiveSkill.ActiveRemaining);
    }
}
