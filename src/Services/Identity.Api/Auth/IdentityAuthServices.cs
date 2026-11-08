using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Identity.Api.Data;
using JabaSurvivol.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Identity.Api.Auth;

public static class AccountValidation
{
    public const int MinimumPasswordLength = 8;
    public const int MaximumPasswordLength = 128;
    public const int MinimumUserNameLength = 3;
    public const int MaximumUserNameLength = 32;

    public static bool IsValidUserName(string? userName, out string detail)
    {
        if (string.IsNullOrWhiteSpace(userName) ||
            userName.Length < MinimumUserNameLength ||
            userName.Length > MaximumUserNameLength)
        {
            detail = $"Username must be {MinimumUserNameLength}-{MaximumUserNameLength} characters.";
            return false;
        }

        foreach (var character in userName)
        {
            var valid = char.IsAsciiLetterOrDigit(character) || character is '_' or '-';
            if (!valid)
            {
                detail = "Username may contain only letters, digits, underscore, and hyphen.";
                return false;
            }
        }

        detail = string.Empty;
        return true;
    }

    public static bool IsValidPassword(string? password, out string detail)
    {
        if (string.IsNullOrWhiteSpace(password) ||
            password.Length < MinimumPasswordLength ||
            password.Length > MaximumPasswordLength)
        {
            detail = $"Password must be {MinimumPasswordLength}-{MaximumPasswordLength} characters.";
            return false;
        }

        var hasLetter = false;
        var hasDigit = false;
        foreach (var character in password)
        {
            hasLetter |= char.IsLetter(character);
            hasDigit |= char.IsDigit(character);
        }

        if (!hasLetter || !hasDigit)
        {
            detail = "Password must contain at least one letter and one digit.";
            return false;
        }

        detail = string.Empty;
        return true;
    }

    public static string Normalize(string userName)
    {
        return userName.ToUpperInvariant();
    }
}

public sealed class PasswordService
{
    private readonly PasswordHasher<Account> _hasher = new();

    public string Hash(Account account, string password)
    {
        return _hasher.HashPassword(account, password);
    }

    public bool Verify(Account account, string password, out bool needsRehash)
    {
        var result = _hasher.VerifyHashedPassword(account, account.PasswordHash, password);
        needsRehash = result == PasswordVerificationResult.SuccessRehashNeeded;
        return result != PasswordVerificationResult.Failed;
    }
}

public sealed class RefreshTokenService(JwtOptions options, TimeProvider timeProvider)
{
    public const int TokenByteLength = 32;

    public RefreshTokenIssue Create()
    {
        var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(TokenByteLength));
        var expiresAt = timeProvider.GetUtcNow().AddDays(options.RefreshTokenDays);
        return new RefreshTokenIssue(token, Hash(token), expiresAt);
    }

    public static string Hash(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
    }

    public int LifetimeSeconds => checked(options.RefreshTokenDays * 86_400);
}

public readonly record struct RefreshTokenIssue(string Token, string Hash, DateTimeOffset ExpiresAt);

public sealed class AccessTokenIssuer(JwtOptions options, TimeProvider timeProvider)
{
    public AccessTokenIssue Create(Guid accountId, string userName)
    {
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.AddMinutes(options.AccessTokenMinutes);
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtClaims.Subject, accountId.ToString("D", CultureInfo.InvariantCulture)),
                new Claim(JwtClaims.UserName, userName),
                new Claim(JwtClaims.TokenId, Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture))
            ]),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
                SecurityAlgorithms.HmacSha256)
        };

        var handler = new JsonWebTokenHandler();
        var token = handler.CreateToken(descriptor);
        return new AccessTokenIssue(token, checked(options.AccessTokenMinutes * 60));
    }
}

public readonly record struct AccessTokenIssue(string Token, int ExpiresInSeconds);
