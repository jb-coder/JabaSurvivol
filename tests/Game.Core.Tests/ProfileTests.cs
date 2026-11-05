namespace JabaSurvivol.Core.Tests;

public sealed class ProfileTests
{
    [Fact]
    public void AddXp_levels_up_and_awards_skill_points()
    {
        var profile = new Profile();

        var gained = profile.AddXp(Profile.XpToNext(1));

        Assert.Equal(1, gained);
        Assert.Equal(2, profile.Level);
        Assert.Equal(0, profile.Xp);
        Assert.Equal(1, profile.SkillPoints);
    }

    [Fact]
    public void AddXp_awards_extra_points_at_milestone_levels()
    {
        var profile = new Profile();

        profile.AddXp(100 + 200 + 300 + 400);

        Assert.Equal(5, profile.Level);
        Assert.Equal(5, profile.SkillPoints);
    }

    [Fact]
    public void AddXp_reaches_level_cap_and_total_skill_points()
    {
        var profile = new Profile();

        profile.AddXp(int.MaxValue);

        Assert.Equal(Profile.MaxLevel, profile.Level);
        Assert.Equal(Profile.MaxSkillPoints, profile.SkillPoints);
        Assert.Equal(0, profile.Xp);
    }

    [Fact]
    public void Supplies_are_added_and_spent_without_going_negative()
    {
        var profile = new Profile();

        profile.AddSupplies(250);

        Assert.Equal(250, profile.Supplies);
        Assert.True(profile.SpendSupplies(100));
        Assert.Equal(150, profile.Supplies);
        Assert.False(profile.SpendSupplies(1000));
        Assert.Equal(150, profile.Supplies);
    }

    [Fact]
    public void RecordRunEnd_keeps_progress_and_updates_records()
    {
        var profile = new Profile { Supplies = 400, Level = 7, SkillPoints = 8 };
        var run = new RunState(GameBalance.PlayerMaxHealth);
        for (var i = 0; i < 7; i++)
        {
            run.RegisterKill();
        }

        profile.RecordRunEnd(run);

        Assert.Equal(400, profile.Supplies);
        Assert.Equal(7, profile.Level);
        Assert.Equal(8, profile.SkillPoints);
        Assert.Equal(1, profile.TotalRuns);
        Assert.Equal(7, profile.TotalKills);
        Assert.Equal(2, profile.HighestRound);
        Assert.Equal(7, profile.BestKills);
    }

    [Fact]
    public void Validate_normalizes_invalid_values()
    {
        var profile = new Profile
        {
            SchemaVersion = 0,
            Supplies = -5,
            Xp = -10,
            Level = 99,
            SkillPoints = 99,
            HighestRound = -1,
            TotalKills = -2,
            TotalRuns = -3,
            BestKills = -4
        };

        profile.Validate();

        Assert.Equal(Profile.CurrentSchemaVersion, profile.SchemaVersion);
        Assert.Equal(0, profile.Supplies);
        Assert.Equal(0, profile.Xp);
        Assert.Equal(Profile.MaxLevel, profile.Level);
        Assert.Equal(Profile.MaxSkillPoints, profile.SkillPoints);
        Assert.Equal(1, profile.HighestRound);
        Assert.Equal(0, profile.TotalKills);
        Assert.Equal(0, profile.TotalRuns);
        Assert.Equal(0, profile.BestKills);
    }
}
