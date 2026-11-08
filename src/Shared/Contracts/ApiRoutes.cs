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
}
