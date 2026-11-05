using JabaSurvivol.Contracts;

namespace JabaSurvivol.IntegrationTests;

public sealed class ApiContractTests
{
    [Fact]
    public void Api_routes_are_versioned()
    {
        Assert.StartsWith("/api/v1/", ApiRoutes.GameStatus, StringComparison.Ordinal);
        Assert.StartsWith("/api/v1/", ApiRoutes.IdentityStatus, StringComparison.Ordinal);
        Assert.StartsWith("/api/v1/", ApiRoutes.BffStatus, StringComparison.Ordinal);
    }
}
