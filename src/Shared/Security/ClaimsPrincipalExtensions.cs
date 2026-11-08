using System.Security.Claims;

namespace JabaSurvivol.Security;

public static class JwtClaims
{
    public const string Subject = "sub";
    public const string UserName = "username";
    public const string TokenId = "jti";
}

public static class ClaimsPrincipalExtensions
{
    public static bool TryGetAccountId(this ClaimsPrincipal principal, out Guid accountId)
    {
        var subject = principal.FindFirstValue(JwtClaims.Subject);
        return Guid.TryParse(subject, out accountId);
    }

    public static Guid GetAccountId(this ClaimsPrincipal principal)
    {
        return principal.TryGetAccountId(out var accountId)
            ? accountId
            : throw new InvalidOperationException("The authenticated principal has no valid account subject.");
    }

    public static string GetUserName(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(JwtClaims.UserName) ?? string.Empty;
    }
}
