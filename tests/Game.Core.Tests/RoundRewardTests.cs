namespace JabaSurvivol.Core.Tests;

public sealed class RoundRewardTests
{
    [Fact]
    public void GenerateOptions_returns_three_distinct_options()
    {
        var options = RoundRewardModel.GenerateOptions(1, 12345);

        Assert.Equal(GameBalance.RoundRewardOptionCount, options.Length);
        Assert.Equal(options.Length, options.Select(option => option.Type).Distinct().Count());
    }

    [Fact]
    public void GenerateOptions_is_deterministic_for_seed_and_round()
    {
        var first = RoundRewardModel.GenerateOptions(4, 987654);
        var second = RoundRewardModel.GenerateOptions(4, 987654);

        Assert.Equal(first, second);
    }

    [Fact]
    public void GenerateOptions_changes_with_the_seed()
    {
        var first = RoundRewardModel.GenerateOptions(1, 1);
        var second = RoundRewardModel.GenerateOptions(1, 2);

        Assert.False(first.SequenceEqual(second));
    }

    [Fact]
    public void GenerateOptions_uses_the_scaled_amount_for_each_type()
    {
        var options = RoundRewardModel.GenerateOptions(4, 7);

        foreach (var option in options)
        {
            Assert.Equal(RoundRewardModel.GetAmount(option.Type, 4), option.Amount);
        }

        Assert.True(
            RoundRewardModel.GetAmount(RoundRewardType.Supplies, 10)
            > RoundRewardModel.GetAmount(RoundRewardType.Supplies, 1));
        Assert.True(
            RoundRewardModel.GetAmount(RoundRewardType.Xp, 10)
            > RoundRewardModel.GetAmount(RoundRewardType.Xp, 1));
    }

    [Fact]
    public void Apply_supplies_adds_persistent_supplies()
    {
        var profile = new Profile();
        var run = new RunState(GameBalance.PlayerMaxHealth);
        var option = new RoundRewardOption(RoundRewardType.Supplies, 75);

        var result = RoundRewardModel.Apply(option, profile, run);

        Assert.Equal(75, profile.Supplies);
        Assert.Equal(75, result.Supplies);
        Assert.Equal(0, result.Xp);
        Assert.Equal(0, run.Ammo);
        Assert.Equal(0, run.Armor);
    }

    [Fact]
    public void Apply_xp_adds_persistent_xp()
    {
        var profile = new Profile();
        var run = new RunState(GameBalance.PlayerMaxHealth);
        var option = new RoundRewardOption(RoundRewardType.Xp, 30);

        var result = RoundRewardModel.Apply(option, profile, run);

        Assert.Equal(30, profile.Xp);
        Assert.Equal(30, result.Xp);
        Assert.Equal(0, profile.Supplies);
    }

    [Fact]
    public void Apply_ammo_adds_only_run_resources()
    {
        var profile = new Profile { Supplies = 200 };
        profile.UnlockWeapon(WeaponId.Smg);
        profile.EquipWeapon(WeaponId.Smg);
        var run = new RunState(GameBalance.PlayerMaxHealth, profile: profile);
        var initialReserve = run.Reserve;
        var option = new RoundRewardOption(RoundRewardType.AmmoCrate, GameBalance.AmmoCrateAmount);

        var result = RoundRewardModel.Apply(option, profile, run);

        Assert.Equal(initialReserve + GameBalance.AmmoCrateAmount, run.Ammo);
        Assert.Equal(initialReserve + GameBalance.AmmoCrateAmount, run.Reserve);
        Assert.Equal(GameBalance.AmmoCrateAmount, result.Ammo);
        Assert.Equal(200, profile.Supplies);
    }

    [Fact]
    public void Apply_armor_adds_only_run_resources()
    {
        var profile = new Profile { Supplies = 200 };
        var run = new RunState(GameBalance.PlayerMaxHealth, maxArmor: 20);
        run.DamagePlayer(15f);
        var option = new RoundRewardOption(RoundRewardType.ArmorPlate, GameBalance.ArmorPlateAmount);

        var result = RoundRewardModel.Apply(option, profile, run);

        Assert.Equal(15, run.Armor);
        Assert.Equal(GameBalance.ArmorPlateAmount, result.Armor);
        Assert.Equal(200, profile.Supplies);
    }

    [Fact]
    public void Apply_armor_reports_only_the_repair_that_was_applied()
    {
        var profile = new Profile();
        var run = new RunState(GameBalance.PlayerMaxHealth, maxArmor: 10);
        run.DamagePlayer(5f);
        var option = new RoundRewardOption(RoundRewardType.ArmorPlate, 10);

        var result = RoundRewardModel.Apply(option, profile, run);

        Assert.Equal(5, result.Armor);
        Assert.Equal(10, run.Armor);
    }

    [Fact]
    public void Persistent_rewards_survive_death_and_run_only_rewards_do_not()
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
        RoundRewardModel.Apply(
            new RoundRewardOption(RoundRewardType.ArmorPlate, GameBalance.ArmorPlateAmount),
            profile,
            run);

        Assert.Equal(initialReserve + GameBalance.AmmoCrateAmount, run.Reserve);

        run.DamagePlayer(GameBalance.PlayerMaxHealth * 2f);
        profile.RecordRunEnd(run);
        run.Restart();

        Assert.Equal(75, profile.Supplies);
        Assert.Equal(30, profile.Xp);
        Assert.Equal(initialReserve, run.Reserve);
        Assert.Equal(0, run.Armor);
    }

    [Fact]
    public void GetAmount_rejects_invalid_rounds_and_types()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RoundRewardModel.GetAmount(RoundRewardType.Supplies, 0));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => RoundRewardModel.GetAmount((RoundRewardType)99, 1));
    }

    [Fact]
    public void GenerateOptions_rejects_invalid_rounds()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RoundRewardModel.GenerateOptions(0, 1));
    }

    [Fact]
    public void Apply_rejects_null_arguments()
    {
        var option = new RoundRewardOption(RoundRewardType.Supplies, 10);
        var profile = new Profile();
        var run = new RunState(GameBalance.PlayerMaxHealth);

        Assert.Throws<ArgumentNullException>(() => RoundRewardModel.Apply(option, null!, run));
        Assert.Throws<ArgumentNullException>(() => RoundRewardModel.Apply(option, profile, null!));
    }
}
