using Identity.Api.Auth;
using Identity.Api.Data;
using JabaSurvivol.Contracts;
using JabaSurvivol.Security;
using Microsoft.EntityFrameworkCore;

namespace Identity.Api.Endpoints;

public static class IdentityEndpoints
{
    private const string AuthenticationRateLimitPolicy = "identity-auth";
    private static readonly PasswordService Passwords = new();
    private static readonly string DummyPasswordHash = Passwords.Hash(
        new Account { Id = Guid.Empty, UserName = "dummy" },
        "DummyPassword1");

    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/identity").WithTags("Identity");

        group.MapPost("/register", RegisterAsync)
            .AllowAnonymous()
            .RequireRateLimiting(AuthenticationRateLimitPolicy)
            .WithName("RegisterAccount");

        group.MapPost("/login", LoginAsync)
            .AllowAnonymous()
            .RequireRateLimiting(AuthenticationRateLimitPolicy)
            .WithName("LoginAccount");

        group.MapPost("/refresh", RefreshAsync)
            .AllowAnonymous()
            .RequireRateLimiting(AuthenticationRateLimitPolicy)
            .WithName("RefreshSession");

        group.MapPost("/logout", LogoutAsync)
            .RequireAuthorization()
            .RequireRateLimiting(AuthenticationRateLimitPolicy)
            .WithName("LogoutSession");

        group.MapGet("/me", MeAsync)
            .RequireAuthorization()
            .WithName("GetCurrentAccount");

        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest? request,
        IdentityDbContext database,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return ApiProblem.Validation("invalid_request", "A request body is required.");
        }

        if (!AccountValidation.IsValidUserName(request.Username, out var userNameError))
        {
            return ApiProblem.Validation("invalid_username", userNameError);
        }

        if (!AccountValidation.IsValidPassword(request.Password, out var passwordError))
        {
            return ApiProblem.Validation("invalid_password", passwordError);
        }

        var normalized = AccountValidation.Normalize(request.Username!);
        var exists = await database.Accounts
            .AnyAsync(account => account.NormalizedUserName == normalized, cancellationToken)
            .ConfigureAwait(false);

        if (exists)
        {
            return UsernameTaken();
        }

        var account = new Account
        {
            Id = Guid.NewGuid(),
            UserName = request.Username!,
            NormalizedUserName = normalized,
            CreatedAt = timeProvider.GetUtcNow()
        };
        account.PasswordHash = Passwords.Hash(account, request.Password!);

        database.Accounts.Add(account);

        try
        {
            await database.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException)
        {
            return UsernameTaken();
        }

        var response = new AccountResponse
        {
            AccountId = account.Id,
            Username = account.UserName
        };

        return Results.Created(ApiRoutes.IdentityMe, response);
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest? request,
        IdentityDbContext database,
        AccessTokenIssuer accessTokens,
        RefreshTokenService refreshTokens,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (request is null ||
            string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return ApiProblem.Unauthorized("Invalid username or password.");
        }

        var normalized = AccountValidation.Normalize(request.Username);
        var account = await database.Accounts
            .SingleOrDefaultAsync(candidate => candidate.NormalizedUserName == normalized, cancellationToken)
            .ConfigureAwait(false);

        if (account is null)
        {
            Passwords.Verify(
                new Account { PasswordHash = DummyPasswordHash },
                request.Password,
                out _);
            return ApiProblem.Unauthorized("Invalid username or password.");
        }

        if (!Passwords.Verify(account, request.Password, out var needsRehash))
        {
            return ApiProblem.Unauthorized("Invalid username or password.");
        }

        if (needsRehash)
        {
            account.PasswordHash = Passwords.Hash(account, request.Password);
        }

        var access = accessTokens.Create(account.Id, account.UserName);
        var refresh = refreshTokens.Create();

        database.RefreshSessions.Add(CreateSession(account.Id, Guid.NewGuid(), refresh, timeProvider));
        await database.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Results.Ok(CreateTokenResponse(account, access, refresh, refreshTokens));
    }

    private static async Task<IResult> RefreshAsync(
        RefreshRequest? request,
        IdentityDbContext database,
        AccessTokenIssuer accessTokens,
        RefreshTokenService refreshTokens,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return ApiProblem.Unauthorized("A refresh token is required.");
        }

        var hash = RefreshTokenService.Hash(request.RefreshToken);
        var session = await database.RefreshSessions
            .SingleOrDefaultAsync(candidate => candidate.TokenHash == hash, cancellationToken)
            .ConfigureAwait(false);

        if (session is null)
        {
            return ApiProblem.Unauthorized("The refresh token is invalid.");
        }

        var now = timeProvider.GetUtcNow();
        if (session.RevokedAt is not null)
        {
            await RevokeFamilyAsync(database, session.FamilyId, now, cancellationToken).ConfigureAwait(false);
            return ApiProblem.Unauthorized("The refresh token was already used. Sign in again.");
        }

        if (session.ExpiresAt <= now)
        {
            return ApiProblem.Unauthorized("The refresh token has expired. Sign in again.");
        }

        var account = await database.Accounts
            .SingleOrDefaultAsync(candidate => candidate.Id == session.AccountId, cancellationToken)
            .ConfigureAwait(false);

        if (account is null)
        {
            await RevokeFamilyAsync(database, session.FamilyId, now, cancellationToken).ConfigureAwait(false);
            return ApiProblem.Unauthorized("The account is no longer available.");
        }

        var nextRefresh = refreshTokens.Create();
        var rotated = await database.RefreshSessions
            .Where(candidate => candidate.Id == session.Id && candidate.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(candidate => candidate.RevokedAt, now)
                    .SetProperty(candidate => candidate.ReplacedByTokenHash, nextRefresh.Hash),
                cancellationToken)
            .ConfigureAwait(false);

        if (rotated == 0)
        {
            await RevokeFamilyAsync(database, session.FamilyId, now, cancellationToken).ConfigureAwait(false);
            return ApiProblem.Unauthorized("The refresh token was already used. Sign in again.");
        }

        database.RefreshSessions.Add(CreateSession(account.Id, session.FamilyId, nextRefresh, timeProvider));
        await database.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var access = accessTokens.Create(account.Id, account.UserName);
        return Results.Ok(CreateTokenResponse(account, access, nextRefresh, refreshTokens));
    }

    private static async Task<IResult> LogoutAsync(
        LogoutRequest? request,
        HttpContext httpContext,
        IdentityDbContext database,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetAccountId(out var accountId))
        {
            return ApiProblem.Unauthorized();
        }

        if (request is null || string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return ApiProblem.Validation("invalid_request", "A refresh token is required.");
        }

        var hash = RefreshTokenService.Hash(request.RefreshToken);
        var session = await database.RefreshSessions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.TokenHash == hash && candidate.AccountId == accountId,
                cancellationToken)
            .ConfigureAwait(false);

        if (session is not null)
        {
            await RevokeFamilyAsync(database, session.FamilyId, timeProvider.GetUtcNow(), cancellationToken)
                .ConfigureAwait(false);
        }

        return Results.NoContent();
    }

    private static async Task<IResult> MeAsync(
        HttpContext httpContext,
        IdentityDbContext database,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetAccountId(out var accountId))
        {
            return ApiProblem.Unauthorized();
        }

        var account = await database.Accounts
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == accountId, cancellationToken)
            .ConfigureAwait(false);

        if (account is null)
        {
            return ApiProblem.Unauthorized("The account is no longer available.");
        }

        return Results.Ok(new AccountResponse
        {
            AccountId = account.Id,
            Username = account.UserName
        });
    }

    private static RefreshSession CreateSession(
        Guid accountId,
        Guid familyId,
        RefreshTokenIssue token,
        TimeProvider timeProvider)
    {
        return new RefreshSession
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            FamilyId = familyId,
            TokenHash = token.Hash,
            CreatedAt = timeProvider.GetUtcNow(),
            ExpiresAt = token.ExpiresAt
        };
    }

    private static TokenResponse CreateTokenResponse(
        Account account,
        AccessTokenIssue access,
        RefreshTokenIssue refresh,
        RefreshTokenService refreshTokens)
    {
        return new TokenResponse
        {
            AccessToken = access.Token,
            ExpiresInSeconds = access.ExpiresInSeconds,
            RefreshToken = refresh.Token,
            RefreshExpiresInSeconds = refreshTokens.LifetimeSeconds,
            AccountId = account.Id,
            Username = account.UserName
        };
    }

    private static async Task RevokeFamilyAsync(
        IdentityDbContext database,
        Guid familyId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await database.RefreshSessions
            .Where(session => session.FamilyId == familyId && session.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(session => session.RevokedAt, now),
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static IResult UsernameTaken()
    {
        return ApiProblem.Create(
            StatusCodes.Status409Conflict,
            "username_taken",
            "Username is not available",
            "Choose a different username.");
    }
}
