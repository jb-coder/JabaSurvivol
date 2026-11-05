using JabaSurvivol.Contracts;

namespace JabaSurvivol.SmokeTests;

public sealed class SmokeContractTests
{
    [Fact]
    public void Health_and_status_routes_are_available()
    {
        Assert.Equal("v1", ApiRoutes.Version);
        Assert.Equal("/api/v1/game/status", ApiRoutes.GameStatus);
        Assert.Equal("/api/v1/identity/status", ApiRoutes.IdentityStatus);
        Assert.Equal("/api/v1/status", ApiRoutes.BffStatus);
    }
}
