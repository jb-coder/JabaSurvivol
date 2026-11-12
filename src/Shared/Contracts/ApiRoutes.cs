namespace JabaSurvivol.Contracts;

public static class ApiRoutes
{
    public const string Version = "v1";
    public const string GameStatus = "/api/v1/game/status";
    public const string IdentityStatus = "/api/v1/identity/status";
    public const string BffStatus = "/api/v1/status";

    public const string IdentityRegister = "/api/v1/identity/register";
    public const string IdentityLogin = "/api/v1/identity/login";
    public const string IdentityRefresh = "/api/v1/identity/refresh";
    public const string IdentityLogout = "/api/v1/identity/logout";
    public const string IdentityMe = "/api/v1/identity/me";

    public const string GameConfig = "/api/v1/game/config";
    public const string CloudSavePrimary = "/api/v1/game/saves/primary";
    public const string LeaderboardSubmit = "/api/v1/game/leaderboards/highest-round/runs";
    public const string LeaderboardTop = "/api/v1/game/leaderboards/highest-round";
    public const string TelemetryEvents = "/api/v1/telemetry/events";
}
