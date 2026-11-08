namespace JabaSurvivol.Core.Tests;

public sealed class SynergyTests
{
    [Fact]
    public void Synergies_require_both_prerequisite_ranks()
    {
        var profile = new Profile();
        profile.SetSkillRank(SkillNode.Strength, 3);
        profile.SetSkillRank(SkillNode.Vitality, 2);

        Assert.False(SynergyCatalog.IsUnlocked(profile, SynergyId.Berserker));

        profile.SetSkillRank(SkillNode.Vitality, 3);
        Assert.True(SynergyCatalog.IsUnlocked(profile, SynergyId.Berserker));
    }

    [Fact]
    public void Berserker_adds_damage_below_half_health()
    {
        var profile = CreateBerserkerProfile();
        var weapon = WeaponUpgradeService.GetEffectiveStats(profile, WeaponId.Pistol);
        var armor = ArmorService.GetEffectiveStats(profile, ArmorTier.None);
        var normal = StatAggregation.Compute(profile, weapon, armor);
        var modifiers = SynergyService.GetTemporaryModifiers(profile, healthFraction: 0.4f, ghostActive: false);
        var berserker = StatAggregation.Compute(profile, weapon, armor, modifiers);
        var fullHealth = SynergyService.GetTemporaryModifiers(profile, healthFraction: 0.6f, ghostActive: false);
        var notBerserker = StatAggregation.Compute(profile, weapon, armor, fullHealth);

        Assert.True(berserker.EffectiveWeaponDamage > normal.EffectiveWeaponDamage);
        Assert.Equal(normal.EffectiveWeaponDamage, notBerserker.EffectiveWeaponDamage, precision: 3);
        Assert.Equal(0.10f, modifiers.DamageBonus, precision: 3);
    }

    [Fact]
    public void Repeated_aggregation_does_not_double_apply_synergies()
    {
        var profile = CreateGhostProfile();
        var weapon = WeaponUpgradeService.GetEffectiveStats(profile, WeaponId.Pistol);
        var armor = ArmorService.GetEffectiveStats(profile, ArmorTier.None);
        var modifiers = SynergyService.GetTemporaryModifiers(profile, healthFraction: 1f, ghostActive: true);

        var first = StatAggregation.Compute(profile, weapon, armor, modifiers);
        var second = StatAggregation.Compute(profile, weapon, armor, modifiers);

        Assert.Equal(first.MoveSpeed, second.MoveSpeed, precision: 4);
        Assert.Equal(1.16f * GameBalance.PlayerMoveSpeed, first.MoveSpeed, precision: 4);
    }

    [Fact]
    public void Ghost_grants_move_speed_for_two_seconds_after_a_kill()
    {
        var profile = CreateGhostProfile();
        var state = new SynergyState();
        state.OnKill(profile);

        Assert.True(state.GhostActive);
        Assert.Equal(2f, state.GhostRemaining, precision: 3);

        state.Tick(1f, profile);
        Assert.True(state.GhostActive);

        state.Tick(1.1f, profile);
        Assert.False(state.GhostActive);
    }

    [Fact]
    public void Ghost_requires_the_synergy_to_activate()
    {
        var profile = new Profile();
        profile.SetSkillRank(SkillNode.Speed, 3);
        profile.SetSkillRank(SkillNode.Precision, 2);
        var state = new SynergyState();

        state.OnKill(profile);

        Assert.False(state.GhostActive);
    }

    [Fact]
    public void Bastion_regenerates_armor_after_the_delay()
    {
        var profile = CreateBastionProfile();
        var run = new RunState(GameBalance.PlayerMaxHealth, maxArmor: 50, profile: profile);
        run.DamagePlayer(20f);
        Assert.Equal(30, run.Armor);

        Assert.Equal(0, run.TickSynergies(4f));
        Assert.Equal(30, run.Armor);

        Assert.Equal(2, run.TickSynergies(1f));
        Assert.Equal(32, run.Armor);
    }

    [Fact]
    public void Taking_damage_resets_the_bastion_regen_delay()
    {
        var profile = CreateBastionProfile();
        var run = new RunState(GameBalance.PlayerMaxHealth, maxArmor: 50, profile: profile);
        run.DamagePlayer(20f);
        run.TickSynergies(4f);
        run.DamagePlayer(5f);

        Assert.Equal(0, run.TickSynergies(4f));
    }

    [Fact]
    public void Restart_resets_synergy_timers()
    {
        var profile = CreateGhostProfile();
        var run = new RunState(GameBalance.PlayerMaxHealth, profile: profile);
        run.Synergy.OnKill(profile);
        run.TickSynergies(0.5f);

        run.Restart();

        Assert.False(run.Synergy.GhostActive);
        Assert.Equal(0f, run.Synergy.OutOfCombatSeconds);
    }

    [Fact]
    public void Death_resets_synergy_timers()
    {
        var profile = CreateGhostProfile();
        var run = new RunState(GameBalance.PlayerMaxHealth, profile: profile);
        run.Synergy.OnKill(profile);
        run.TickSynergies(0.5f);

        run.DamagePlayer(GameBalance.PlayerMaxHealth * 2f);

        Assert.True(run.IsGameOver);
        Assert.False(run.Synergy.GhostActive);
        Assert.Equal(0f, run.Synergy.OutOfCombatSeconds);
    }

    private static Profile CreateBerserkerProfile()
    {
        var profile = new Profile();
        profile.SetSkillRank(SkillNode.Strength, 3);
        profile.SetSkillRank(SkillNode.Vitality, 3);
        return profile;
    }

    private static Profile CreateGhostProfile()
    {
        var profile = new Profile();
        profile.SetSkillRank(SkillNode.Speed, 3);
        profile.SetSkillRank(SkillNode.Precision, 3);
        return profile;
    }

    private static Profile CreateBastionProfile()
    {
        var profile = new Profile();
        profile.SetSkillRank(SkillNode.Endurance, 3);
        profile.SetSkillRank(SkillNode.Engineering, 3);
        return profile;
    }
}
