namespace JabaSurvivol.Core.Tests;

using System.Globalization;

public sealed class BalanceSimulationTests
{
    [Fact]
    public void Run_covers_rounds_1_to_10()
    {
        var report = BalanceSimulation.Run();

        Assert.Equal(GameBalance.MaxRound, report.Rounds.Count);
        Assert.Equal(1, report.Rounds[0].Round);
        Assert.Equal(GameBalance.MaxRound, report.Rounds[^1].Round);
    }

    [Fact]
    public void Run_is_reproducible_for_a_seed()
    {
        var first = BalanceSimulation.Run(new BalanceSimulationOptions { Seed = 4242 });
        var second = BalanceSimulation.Run(new BalanceSimulationOptions { Seed = 4242 });

        Assert.Equal(first.TotalSuppliesEarned, second.TotalSuppliesEarned);
        Assert.Equal(first.TotalSuppliesSpent, second.TotalSuppliesSpent);
        Assert.Equal(first.TotalXp, second.TotalXp);
        Assert.Equal(first.FinalLevel, second.FinalLevel);
        Assert.Equal(first.FinalSkillPoints, second.FinalSkillPoints);
        Assert.Equal(first.Outliers.Count, second.Outliers.Count);
        Assert.Equal(first.Rounds.Count, second.Rounds.Count);

        for (var i = 0; i < first.Rounds.Count; i++)
        {
            Assert.Equal(first.Rounds[i].Reward.Type, second.Rounds[i].Reward.Type);
            Assert.Equal(first.Rounds[i].SuppliesEarned, second.Rounds[i].SuppliesEarned);
            Assert.Equal(first.Rounds[i].XpEarned, second.Rounds[i].XpEarned);
            Assert.Equal(first.Rounds[i].Level, second.Rounds[i].Level);
            Assert.Equal(first.Rounds[i].SkillPoints, second.Rounds[i].SkillPoints);
            Assert.Equal(first.Rounds[i].Build.Weapon, second.Rounds[i].Build.Weapon);
            Assert.Equal(first.Rounds[i].Build.Armor, second.Rounds[i].Build.Armor);
            Assert.Equal(first.Rounds[i].Build.DamageTier, second.Rounds[i].Build.DamageTier);
            Assert.Equal(first.Rounds[i].Build.FireRateTier, second.Rounds[i].Build.FireRateTier);
            Assert.Equal(first.Rounds[i].Build.MagazineTier, second.Rounds[i].Build.MagazineTier);
            Assert.Equal(first.Rounds[i].Build.ReloadTier, second.Rounds[i].Build.ReloadTier);
            Assert.Equal(first.Rounds[i].Build.StrengthRank, second.Rounds[i].Build.StrengthRank);
            Assert.Equal(first.Rounds[i].Build.VitalityRank, second.Rounds[i].Build.VitalityRank);
            Assert.Equal(first.Rounds[i].Content.Walkers, second.Rounds[i].Content.Walkers);
            Assert.Equal(first.Rounds[i].Content.Runners, second.Rounds[i].Content.Runners);
            Assert.Equal(first.Rounds[i].Content.Brutes, second.Rounds[i].Content.Brutes);
            Assert.Equal(first.Rounds[i].Ammo.Shots, second.Rounds[i].Ammo.Shots);
            Assert.Equal(first.Rounds[i].Ammo.Reloads, second.Rounds[i].Ammo.Reloads);
            Assert.Equal(first.Rounds[i].Ammo.Switches, second.Rounds[i].Ammo.Switches);
            Assert.Equal(first.Rounds[i].Skills.Selected, second.Rounds[i].Skills.Selected);
            Assert.Equal(first.Rounds[i].Skills.TurretUptime, second.Rounds[i].Skills.TurretUptime);
            Assert.Equal(first.Rounds[i].TotalShots, second.Rounds[i].TotalShots);
        }
    }

    [Fact]
    public void Run_reports_round_pacing_economy_and_build_progress()
    {
        var report = BalanceSimulation.Run();
        var last = report.Rounds[^1];

        Assert.True(report.TotalSuppliesEarned > 0);
        Assert.True(report.TotalSuppliesSpent > 0);
        Assert.True(report.TotalXp > 0);
        Assert.True(report.FinalLevel >= 2);
        Assert.True(last.CumulativeXp > report.Rounds[0].CumulativeXp);
        Assert.Equal(WeaponId.Rifle, last.Build.Weapon);
        Assert.True(last.Build.HasRifle);
        Assert.False(last.Build.HasSmg);
        Assert.False(last.Build.HasShotgun);
        Assert.False(last.Build.HasLmg);
        Assert.True(last.Build.DamageTier > 0);
        Assert.True(last.Build.FireRateTier > 0);
        Assert.Equal(ArmorTier.Medium, last.Build.Armor);
        Assert.True(last.Build.TotalArmor >= last.Build.ArmorTierAbsorb);
        Assert.True(last.Build.StrengthRank > 0);
        Assert.True(last.Build.VitalityRank > 0);
    }

    [Fact]
    public void Run_keeps_every_build_inside_the_documented_caps()
    {
        var report = BalanceSimulation.Run();

        Assert.All(report.Rounds, round =>
        {
            var stats = round.Build.Stats;
            Assert.True(stats.WeaponDamageMultiplier <= 1f + StatCaps.DamageBonus + 0.0001f);
            Assert.True(stats.MoveSpeed <= GameBalance.PlayerMoveSpeed * (1f + StatCaps.MoveSpeedBonus) + 0.0001f);
            Assert.True(stats.Armor <= StatCaps.Armor);
            Assert.True(stats.CriticalChance <= StatCaps.CriticalChance + 0.0001f);
            Assert.True(stats.HeadshotBonus <= StatCaps.HeadshotBonus + 0.0001f);
        });
    }

    [Fact]
    public void Run_detects_over_cap_raw_builds()
    {
        var profile = new Profile();
        var weapon = WeaponCatalog.GetStats(WeaponId.Pistol);
        var armor = new ArmorStats(ArmorTier.None, 0, 0, 0, 0, 0, 0);
        var temporary = new TemporaryStatModifiers
        {
            MoveSpeedBonus = 10f,
            DamageBonus = 10f,
            ArmorBonus = 1000,
            CriticalChanceBonus = 10f,
            HeadshotBonusBonus = 10f
        };

        var raw = StatAggregation.ComputeRaw(profile, weapon, armor, temporary);
        var capped = StatAggregation.Compute(profile, weapon, armor, temporary);

        Assert.True(BalanceSimulation.HasCapViolation(raw, out var reason));
        Assert.False(string.IsNullOrWhiteSpace(reason));
        Assert.False(BalanceSimulation.HasCapViolation(capped, out _));
    }

    [Fact]
    public void Run_models_content_ammo_active_skills_and_synergies()
    {
        var profile = new Profile { Level = 30, Supplies = 5000 };
        profile.SetSkillRank(SkillNode.Strength, 3);
        profile.SetSkillRank(SkillNode.Vitality, 3);
        profile.SetSkillRank(SkillNode.Speed, 3);
        profile.SetSkillRank(SkillNode.Precision, 3);
        profile.SetSkillRank(SkillNode.Endurance, 3);
        profile.SetSkillRank(SkillNode.Engineering, 3);
        profile.SetSkillRank(SkillNode.Turret, 3);
        profile.SetSkillRank(SkillNode.Shockwave, 3);
        profile.SetSkillRank(SkillNode.Dash, 3);
        profile.ActiveSkill = SkillNode.Turret;

        var report = BalanceSimulation.Run(new BalanceSimulationOptions
        {
            MaxRound = 1,
            StartingProfile = profile
        });
        var round = report.Rounds[0];

        Assert.Equal(
            round.SpawnBudget,
            round.Content.Walkers + round.Content.Runners + round.Content.Brutes);
        Assert.True(round.Ammo.Shots > 0);
        Assert.True(round.Skills.TurretUptime > 0f);
        Assert.True(round.Skills.DashUptime > 0f);
        Assert.True(round.Skills.ShockwaveUptime > 0f);
        Assert.True(round.Skills.BerserkerUptime > 0f);
        Assert.True(round.Skills.GhostUptime > 0f);
        Assert.True(round.Skills.BastionUptime > 0f);
        Assert.True(round.Content.BossSeconds > 0f);
    }

    [Fact]
    public void Run_models_the_boss_in_round_ten()
    {
        var report = BalanceSimulation.Run();
        var boss = report.Rounds[^1];

        Assert.True(boss.IsBoss);
        Assert.True(boss.Content.BossSeconds > 0f);
        Assert.True(boss.SuppliesEarned >= 1000);
        Assert.True(boss.XpEarned >= 500);
    }

    [Fact]
    public void Run_flags_ammo_starvation_with_finite_reserves()
    {
        var profile = new Profile();
        profile.UnlockWeapon(WeaponId.Smg);
        profile.EquipWeapon(WeaponId.Smg);

        var report = BalanceSimulation.Run(new BalanceSimulationOptions
        {
            MaxRound = 5,
            StartingProfile = profile
        });

        Assert.Contains(report.Rounds, round => round.Ammo.Starved);
        Assert.Contains(report.Outliers, outlier => outlier.Contains("ammo-starved", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Run_formats_outliers_with_invariant_decimals()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("es-ES");
            var report = BalanceSimulation.Run(new BalanceSimulationOptions
            {
                BossMinimumSeconds = 1000f
            });

            Assert.Contains(report.Outliers, outlier => outlier.Contains("1000.0"));
            Assert.DoesNotContain(report.Outliers, outlier => outlier.Contains("1000,0"));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Run_flags_trivial_elites_level_cap_and_missing_unlocks()
    {
        var maxed = new Profile { Level = 30, Supplies = 10000 };
        maxed.UnlockWeapon(WeaponId.Rifle);
        maxed.EquipWeapon(WeaponId.Rifle);
        for (var tier = 1; tier <= 5; tier++)
        {
            maxed.SetWeaponUpgradeTier(WeaponId.Rifle, WeaponUpgradeStat.Damage, tier);
        }

        maxed.SetSkillRank(SkillNode.Strength, 5);

        var elite = BalanceSimulation.Run(new BalanceSimulationOptions
        {
            MaxRound = 5,
            StartingProfile = maxed
        });
        var levelCap = BalanceSimulation.Run(new BalanceSimulationOptions
        {
            MaxRound = 1,
            StartingProfile = new Profile { Level = 30 }
        });
        var missing = BalanceSimulation.Run(new BalanceSimulationOptions
        {
            MaxRound = 1,
            StartingProfile = new Profile()
        });

        Assert.Contains(elite.Outliers, outlier => outlier.Contains("elite encounter is trivial", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(levelCap.Outliers, outlier => outlier.Contains("Level cap", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(missing.Outliers, outlier => outlier.Contains("No weapon unlock", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(missing.Outliers, outlier => outlier.Contains("No weapon damage upgrade", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Run_flags_boss_and_ammo_outliers()
    {
        var easyBoss = BalanceSimulation.Run(new BalanceSimulationOptions { BossMinimumSeconds = 1000f });
        var hardBoss = BalanceSimulation.Run(new BalanceSimulationOptions { BossMaximumSeconds = 1f });
        var ammo = BalanceSimulation.Run(new BalanceSimulationOptions { AmmoPressureThreshold = 0f });

        Assert.Contains(easyBoss.Outliers, outlier => outlier.Contains("Boss is too easy", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(hardBoss.Outliers, outlier => outlier.Contains("Boss is too hard", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(ammo.Outliers, outlier => outlier.Contains("ammo-starved", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Run_flags_unreachable_synergies_for_a_short_run()
    {
        var report = BalanceSimulation.Run(new BalanceSimulationOptions
        {
            MaxRound = 1,
            StartingProfile = new Profile()
        });

        Assert.Contains(
            report.Outliers,
            outlier => outlier.Contains("synergy", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Run_reports_ammo_crates_and_archetype_mix()
    {
        var report = BalanceSimulation.Run();
        var fourth = report.Rounds[3];
        var tenth = report.Rounds[^1];

        Assert.Contains(report.Rounds, round => round.Reward.Type == RoundRewardType.AmmoCrate);
        Assert.True(fourth.Content.Runners > 0);
        Assert.True(fourth.Content.Brutes > 0);
        Assert.True(tenth.IsBoss);
        Assert.True(tenth.TotalShots >= tenth.ShotsToKill);
    }

    [Fact]
    public void Starting_ammo_crate_bonus_changes_finite_reserve_outcomes()
    {
        var without = BalanceSimulation.Run(new BalanceSimulationOptions
        {
            MaxRound = 5,
            StartingProfile = CreateSmgProfile()
        });
        var with = BalanceSimulation.Run(new BalanceSimulationOptions
        {
            MaxRound = 5,
            StartingProfile = CreateSmgProfile(),
            StartingAmmoCrateBonus = 1000
        });

        Assert.Contains(without.Rounds, round => round.Ammo.Starved);
        Assert.DoesNotContain(with.Rounds, round => round.Ammo.Starved);
    }

    [Fact]
    public void Run_reports_no_outliers_for_the_default_balance()
    {
        var report = BalanceSimulation.Run();

        Assert.True(report.Rounds.All(round => round.ShotsToKill > 1));
        Assert.DoesNotContain(report.Outliers, outlier => outlier.Contains("trivial", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(report.Outliers, outlier => outlier.Contains("unreachable", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(report.Outliers, outlier => outlier.Contains("runaway", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(report.Outliers, outlier => outlier.Contains("cap", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(report.Outliers, outlier => outlier.Contains("Boss is too", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(report.Outliers, outlier => outlier.Contains("ammo-starved", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(report.Outliers, outlier => outlier.Contains("synergy", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Run_stays_within_the_target_run_length()
    {
        var report = BalanceSimulation.Run();
        var totalSeconds = report.Rounds.Sum(round => round.RoundSeconds);

        Assert.InRange(totalSeconds, BalanceTargets.MinimumRunSeconds, BalanceTargets.MaximumRunSeconds);
    }

    [Fact]
    public void Run_keeps_every_round_within_its_pacing_target()
    {
        var report = BalanceSimulation.Run();

        Assert.All(report.Rounds, round =>
        {
            if (round.Round <= 3)
            {
                Assert.InRange(round.RoundSeconds, 0f, BalanceTargets.EarlyRoundMaximumSeconds);
                return;
            }

            if (round.Round <= 7)
            {
                Assert.InRange(round.RoundSeconds, BalanceTargets.MidRoundMinimumSeconds, BalanceTargets.MidRoundMaximumSeconds);
                return;
            }

            Assert.InRange(round.RoundSeconds, BalanceTargets.LateRoundMinimumSeconds, BalanceTargets.LateRoundMaximumSeconds);
        });
    }

    [Fact]
    public void Run_keeps_progression_rewards_within_target_ranges()
    {
        var report = BalanceSimulation.Run();

        Assert.InRange(report.FinalLevel, BalanceTargets.MinimumFinalLevel, BalanceTargets.MaximumFinalLevel);
        Assert.InRange(report.TotalSuppliesEarned, BalanceTargets.MinimumSuppliesEarned, BalanceTargets.MaximumSuppliesEarned);
        Assert.InRange(report.TotalSupplies, BalanceTargets.MinimumSuppliesRemaining, BalanceTargets.MaximumSuppliesRemaining);
    }

    [Fact]
    public void Run_keeps_the_boss_and_ammo_pressure_within_target_ranges()
    {
        var report = BalanceSimulation.Run();
        var boss = report.Rounds[^1];

        Assert.InRange(boss.Content.BossSeconds, BalanceTargets.MinimumBossSeconds, BalanceTargets.MaximumBossSeconds);
        Assert.All(report.Rounds, round => Assert.InRange(round.Ammo.Pressure, 0f, BalanceTargets.MaximumAmmoPressure));
    }

    [Fact]
    public void Run_flags_unreachable_upgrades_when_income_cannot_cover_the_maximum_cost()
    {
        var report = BalanceSimulation.Run(new BalanceSimulationOptions { MaxRound = 1 });

        Assert.Contains(report.Outliers, outlier => outlier.Contains("unreachable", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Run_flags_runaway_supplies_when_the_threshold_is_low()
    {
        var report = BalanceSimulation.Run(new BalanceSimulationOptions { RunawaySupplyThreshold = 1 });

        Assert.Contains(report.Outliers, outlier => outlier.Contains("runaway", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Weapon_scenarios_cover_every_weapon_without_replacing_the_equipped_weapon()
    {
        var scenarios = BalanceSimulation.RunWeaponScenarios();

        Assert.Equal(WeaponCatalog.All.Count, scenarios.Count);
        foreach (var scenario in scenarios)
        {
            Assert.Equal(GameBalance.MaxRound, scenario.Report.Rounds.Count);
            Assert.All(
                scenario.Report.Rounds,
                round => Assert.Equal(scenario.Weapon, round.Build.Weapon));
        }
    }

    [Fact]
    public void Weapon_scenarios_keep_the_rifle_progression_clean()
    {
        var rifle = BalanceSimulation.RunWeaponScenarios()
            .Single(scenario => scenario.Weapon == WeaponId.Rifle);

        Assert.Empty(rifle.Report.Outliers);
    }

    [Fact]
    public void Run_rejects_invalid_options()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => BalanceSimulation.Run(new BalanceSimulationOptions { StartingRound = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => BalanceSimulation.Run(new BalanceSimulationOptions { StartingRound = 5, MaxRound = 4 }));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => BalanceSimulation.Run(new BalanceSimulationOptions { MaxRound = GameBalance.MaxRound + 1 }));
    }

    private static Profile CreateSmgProfile()
    {
        var profile = new Profile();
        profile.UnlockWeapon(WeaponId.Smg);
        profile.EquipWeapon(WeaponId.Smg);
        return profile;
    }
}
