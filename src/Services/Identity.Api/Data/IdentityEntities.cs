namespace Identity.Api.Data;

public sealed class Account
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string NormalizedUserName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class RefreshSession
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public Guid FamilyId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }

    public Account? Account { get; set; }
}
