using Yarp.ReverseProxy.Configuration;

namespace Bff;

public static class ReverseProxyConfiguration
{
    public const string AuthenticatedPolicy = "authenticated";
    public const string IdentityCluster = "identity";
    public const string GameCluster = "game";

    public static IReadOnlyList<RouteConfig> CreateRoutes()
    {
        return
        [
            AnonymousRoute("identity-register", "/api/v1/identity/register", IdentityCluster, "POST"),
            AnonymousRoute("identity-login", "/api/v1/identity/login", IdentityCluster, "POST"),
            AnonymousRoute("identity-refresh", "/api/v1/identity/refresh", IdentityCluster, "POST"),
            ProtectedRoute("identity-logout", "/api/v1/identity/logout", IdentityCluster, "POST"),
            ProtectedRoute("identity-me", "/api/v1/identity/me", IdentityCluster, "GET"),
            AnonymousRoute("game-config", "/api/v1/game/config", GameCluster, "GET"),
            ProtectedRoute("game-saves", "/api/v1/game/saves/primary", GameCluster, "GET", "PUT"),
            ProtectedRoute(
                "leaderboard-submit",
                "/api/v1/game/leaderboards/highest-round/runs",
                GameCluster,
                "POST"),
            AnonymousRoute(
                "leaderboard-top",
                "/api/v1/game/leaderboards/highest-round",
                GameCluster,
                "GET"),
            ProtectedRoute("telemetry-events", "/api/v1/telemetry/events", GameCluster, "POST")
        ];
    }

    public static IReadOnlyList<ClusterConfig> CreateClusters(IConfiguration configuration)
    {
        var identity = configuration["Upstreams:Identity"] ?? "http://identity-api";
        var game = configuration["Upstreams:Game"] ?? "http://game-api";

        return
        [
            Cluster(IdentityCluster, identity),
            Cluster(GameCluster, game)
        ];
    }

    private static RouteConfig AnonymousRoute(string routeId, string path, string clusterId, params string[] methods)
    {
        return new RouteConfig
        {
            RouteId = routeId,
            ClusterId = clusterId,
            Match = new RouteMatch { Path = path, Methods = methods }
        };
    }

    private static RouteConfig ProtectedRoute(string routeId, string path, string clusterId, params string[] methods)
    {
        return new RouteConfig
        {
            RouteId = routeId,
            ClusterId = clusterId,
            Match = new RouteMatch { Path = path },
            AuthorizationPolicy = AuthenticatedPolicy
        };
    }

    private static ClusterConfig Cluster(string clusterId, string address)
    {
        return new ClusterConfig
        {
            ClusterId = clusterId,
            Destinations = new Dictionary<string, DestinationConfig>(StringComparer.Ordinal)
            {
                ["primary"] = new DestinationConfig { Address = address }
            }
        };
    }
}
