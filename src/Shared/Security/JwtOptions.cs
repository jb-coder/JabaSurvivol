namespace JabaSurvivol.Security;

/// <summary>
/// JWT settings shared by the token issuer and every service that validates
/// access tokens. Values come from configuration and are never committed.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public const int MinimumSigningKeyLength = 32;

    public string Issuer { get; set; } = "JabaSurvivol";
    public string Audience { get; set; } = "JabaSurvivol";
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 30;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Issuer))
        {
            throw new InvalidOperationException("Jwt:Issuer must be configured.");
        }

        if (string.IsNullOrWhiteSpace(Audience))
        {
            throw new InvalidOperationException("Jwt:Audience must be configured.");
        }

        if (string.IsNullOrWhiteSpace(SigningKey) || SigningKey.Length < MinimumSigningKeyLength)
        {
            throw new InvalidOperationException(
                $"Jwt:SigningKey must be configured with at least {MinimumSigningKeyLength} characters.");
        }

        if (AccessTokenMinutes <= 0 || AccessTokenMinutes > 60)
        {
            throw new InvalidOperationException("Jwt:AccessTokenMinutes must be between 1 and 60.");
        }

        if (RefreshTokenDays <= 0 || RefreshTokenDays > 90)
        {
            throw new InvalidOperationException("Jwt:RefreshTokenDays must be between 1 and 90.");
        }
    }
}
