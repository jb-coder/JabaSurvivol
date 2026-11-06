namespace JabaSurvivol.Core.Tests;

public sealed class CombatMathTests
{
    [Fact]
    public void TryNormalizeStick_rejects_dead_zone()
    {
        var accepted = CombatMath.TryNormalizeStick(0.01f, 0.01f, 0.16f, out var x, out var y);

        Assert.False(accepted);
        Assert.Equal(0f, x);
        Assert.Equal(0f, y);
    }

    [Fact]
    public void TryNormalizeStick_keeps_diagonal_input_unit_length()
    {
        var accepted = CombatMath.TryNormalizeStick(1f, 1f, 0.16f, out var x, out var y);

        Assert.True(accepted);
        Assert.InRange(MathF.Sqrt((x * x) + (y * y)), 0.999f, 1.001f);
    }

    [Fact]
    public void ClampToArena_keeps_actor_inside_bounds()
    {
        CombatMath.ClampToArena(20f, -20f, 8f, 4f, 0.5f, out var x, out var y);

        Assert.Equal(7.5f, x);
        Assert.Equal(-3.5f, y);
    }

    [Fact]
    public void CirclesOverlap_detects_touching_and_separate_circles()
    {
        Assert.True(CombatMath.CirclesOverlap(0f, 0f, 0.5f, 0.9f, 0f, 0.5f));
        Assert.False(CombatMath.CirclesOverlap(0f, 0f, 0.5f, 2f, 0f, 0.5f));
    }

    [Fact]
    public void ApplyDamageMultiplier_never_returns_negative_damage()
    {
        Assert.Equal(20f, CombatMath.ApplyDamageMultiplier(10f, 2f));
        Assert.Equal(0f, CombatMath.ApplyDamageMultiplier(10f, -1f));
        Assert.Equal(0f, CombatMath.ApplyDamageMultiplier(-10f, 2f));
    }
}
