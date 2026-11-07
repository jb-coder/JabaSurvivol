namespace JabaSurvivol.Core.Tests;

public sealed class SkillTests
{
    [Fact]
    public void Catalog_exposes_nine_nodes_across_three_branches()
    {
        Assert.Equal(9, SkillCatalog.All.Count);
        Assert.Equal(3, SkillCatalog.All.Select(node => node.Branch).Distinct().Count());
        Assert.Equal(3, SkillCatalog.All.Count(node => node.Branch == SkillBranch.Strength));
        Assert.Equal(3, SkillCatalog.All.Count(node => node.Branch == SkillBranch.Agility));
        Assert.Equal(3, SkillCatalog.All.Count(node => node.Branch == SkillBranch.Survival));
        Assert.All(SkillCatalog.All, node => Assert.Equal(5, node.MaxRanks));
    }

    [Fact]
    public void Earned_skill_points_follow_the_level_curve()
    {
        Assert.Equal(0, Profile.GetEarnedSkillPoints(1));
        Assert.Equal(5, Profile.GetEarnedSkillPoints(5));
        Assert.Equal(11, Profile.GetEarnedSkillPoints(10));
        Assert.Equal(23, Profile.GetEarnedSkillPoints(20));
        Assert.Equal(35, Profile.GetEarnedSkillPoints(30));
        Assert.Equal(35, Profile.GetEarnedSkillPoints(99));
    }

    [Fact]
    public void Allocate_spends_points_and_stops_at_max_rank()
    {
        var profile = new Profile { Level = 2, SkillPoints = 1 };

        Assert.True(profile.AllocateSkill(SkillNode.Strength));
        Assert.Equal(1, profile.GetSkillRank(SkillNode.Strength));
        Assert.Equal(0, profile.SkillPoints);
        Assert.False(profile.AllocateSkill(SkillNode.Strength));

        profile.SkillPoints = 10;
        for (var rank = 1; rank < 5; rank++)
        {
            Assert.True(profile.AllocateSkill(SkillNode.Strength));
        }

        Assert.Equal(5, profile.GetSkillRank(SkillNode.Strength));
        Assert.False(profile.AllocateSkill(SkillNode.Strength));
        Assert.Equal(6, profile.SkillPoints);
    }

    [Fact]
    public void Allocate_rejects_unknown_nodes()
    {
        var profile = new Profile { Level = 2, SkillPoints = 1 };

        Assert.False(profile.AllocateSkill((SkillNode)99));
        Assert.Equal(1, profile.SkillPoints);
    }

    [Fact]
    public void Respec_refunds_every_point_without_touching_progress_or_unlocks()
    {
        var profile = new Profile { Level = 10, SkillPoints = 11 };
        profile.AddSupplies(2000);
        ArmoryService.PurchaseWeapon(profile, WeaponId.Smg);
        ArmorService.PurchaseArmor(profile, ArmorTier.Light);

        Assert.True(profile.AllocateSkill(SkillNode.Strength));
        Assert.True(profile.AllocateSkill(SkillNode.Vitality));
        Assert.True(profile.AllocateSkill(SkillNode.Speed));

        var refunded = profile.RespecSkills();

        Assert.Equal(3, refunded);
        Assert.Equal(11, profile.SkillPoints);
        Assert.Equal(0, profile.GetSkillRank(SkillNode.Strength));
        Assert.Equal(10, profile.Level);
        Assert.True(profile.HasWeapon(WeaponId.Smg));
        Assert.True(profile.HasArmor(ArmorTier.Light));
    }

    [Fact]
    public void Validate_clamps_and_deduplicates_skill_ranks()
    {
        var profile = new Profile
        {
            Level = 10,
            SkillPoints = 99,
            AllocatedSkills = new List<SkillAllocation>
            {
                new SkillAllocation(SkillNode.Strength, 99),
                new SkillAllocation(SkillNode.Strength, 2),
                new SkillAllocation((SkillNode)99, 1),
                new SkillAllocation(SkillNode.Speed, 0)
            }
        };

        profile.Validate();

        Assert.Single(profile.AllocatedSkills);
        Assert.Equal(5, profile.GetSkillRank(SkillNode.Strength));
        Assert.Equal(6, profile.SkillPoints);
        Assert.Equal(SkillNode.Turret, profile.ActiveSkill);
    }

    [Fact]
    public void Stat_aggregation_combines_skills_weapon_armor_and_temporary_buffs()
    {
        var profile = new Profile();
        profile.SetSkillRank(SkillNode.Strength, 5);
        profile.SetSkillRank(SkillNode.Vitality, 5);
        profile.SetSkillRank(SkillNode.Speed, 5);
        profile.SetSkillRank(SkillNode.Precision, 5);
        profile.SetSkillRank(SkillNode.Endurance, 5);
        profile.SetSkillRank(SkillNode.Engineering, 5);

        var weapon = WeaponCatalog.GetStats(WeaponId.Pistol);
        var armor = new ArmorStats(ArmorTier.Light, 25, 25, 0, 0, 5, 100);
        var stats = StatAggregation.Compute(profile, weapon, armor);

        Assert.Equal(150f, stats.MaxHealth, 3);
        Assert.Equal(4.95f, stats.MoveSpeed, 3);
        Assert.Equal(1.15f, stats.WeaponDamageMultiplier, 3);
        Assert.Equal(23f, stats.EffectiveWeaponDamage, 3);
        Assert.Equal(0.05f, stats.CriticalChance, 3);
        Assert.Equal(0.25f, stats.HeadshotBonus, 3);
        Assert.Equal(50, stats.Armor);
        Assert.Equal(1.25f, stats.ReloadSpeedMultiplier, 3);
        Assert.Equal(1.25f, stats.AmmoCapacityMultiplier, 3);
    }

    [Fact]
    public void Stat_aggregation_enforces_documented_caps()
    {
        var profile = new Profile();
        var weapon = WeaponCatalog.GetStats(WeaponId.Pistol);
        var armor = new ArmorStats(ArmorTier.Heavy, 90, 90, 1600, 0, 5, 350);
        var temporary = new TemporaryStatModifiers
        {
            MoveSpeedBonus = 10f,
            DamageBonus = 10f,
            ArmorBonus = 1000,
            CriticalChanceBonus = 10f,
            HeadshotBonusBonus = 10f
        };

        var stats = StatAggregation.Compute(profile, weapon, armor, temporary);

        Assert.Equal(4.5f * 1.5f, stats.MoveSpeed, 3);
        Assert.Equal(3f, stats.WeaponDamageMultiplier, 3);
        Assert.Equal(60f, stats.EffectiveWeaponDamage, 3);
        Assert.Equal(0.5f, stats.CriticalChance, 3);
        Assert.Equal(1f, stats.HeadshotBonus, 3);
        Assert.Equal(StatCaps.Armor, stats.Armor);
    }

    [Fact]
    public void Skills_and_unlocks_survive_death()
    {
        var profile = new Profile();
        profile.AddXp(int.MaxValue);
        while (profile.AllocateSkill(SkillNode.Strength))
        {
        }

        profile.AddSupplies(2000);
        ArmoryService.PurchaseWeapon(profile, WeaponId.Rifle);
        ArmorService.PurchaseArmor(profile, ArmorTier.Medium);
        ArmorService.EquipArmor(profile, ArmorTier.Medium);

        var run = new RunState(GameBalance.PlayerMaxHealth);
        run.DamagePlayer(GameBalance.PlayerMaxHealth * 2f);
        profile.RecordRunEnd(run);

        Assert.Equal(5, profile.GetSkillRank(SkillNode.Strength));
        Assert.Equal(30, profile.Level);
        Assert.True(profile.HasWeapon(WeaponId.Rifle));
        Assert.Equal(ArmorTier.Medium, profile.EquippedArmor);
    }

    [Fact]
    public void Active_skill_defaults_to_turret()
    {
        var profile = new Profile();

        Assert.Equal(SkillNode.Turret, profile.ActiveSkill);

        profile.ActiveSkill = SkillNode.Strength;
        profile.Validate();

        Assert.Equal(SkillNode.Turret, profile.ActiveSkill);
    }
}
