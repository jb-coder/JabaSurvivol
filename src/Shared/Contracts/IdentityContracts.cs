namespace JabaSurvivol.Contracts;

public sealed class RegisterRequest
{
    public string? Username { get; set; }
    public string? Password { get; set; }
}

public sealed class LoginRequest
{
    public string? Username { get; set; }
    public string? Password { get; set; }
}

public sealed class RefreshRequest
{
    public string? RefreshToken { get; set; }
}

public sealed class LogoutRequest
{
    public string? RefreshToken { get; set; }
}

public sealed class AccountResponse
{
    public Guid AccountId { get; set; }
    public string Username { get; set; } = string.Empty;
}

public sealed class TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public int ExpiresInSeconds { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public int RefreshExpiresInSeconds { get; set; }
    public Guid AccountId { get; set; }
    public string Username { get; set; } = string.Empty;
}
