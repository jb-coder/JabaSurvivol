using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using JabaSurvivol.Contracts;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace JabaSurvivol.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class IdentityEndpointsTests(PostgresFixture fixture) : IAsyncLifetime
{
    private IdentityApiFactory _factory = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await fixture.CreateDatabaseAsync("identity");
        _factory = new IdentityApiFactory(connectionString).ApplyEnvironment();
        await _factory.InitializeDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Register_login_refresh_and_me_flow_succeeds()
    {
        using var client = _factory.CreateClient();
        var account = await RegisterAsync(client, "Survivor_01", "Password1");

        var login = await LoginAsync(client, "survivor_01", "Password1");
        Assert.False(string.IsNullOrWhiteSpace(login.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(login.RefreshToken));
        Assert.Equal(900, login.ExpiresInSeconds);
        Assert.Equal(2_592_000, login.RefreshExpiresInSeconds);
        Assert.Equal(account.AccountId, login.AccountId);
        Assert.Equal("Survivor_01", login.Username);

        var issuedAt = ReadNumericClaim(login.AccessToken, "iat");
        var expires = ReadNumericClaim(login.AccessToken, "exp");
        Assert.Equal(900, expires - issuedAt);

        using var meRequest = new HttpRequestMessage(HttpMethod.Get, ApiRoutes.IdentityMe);
        meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var meResponse = await client.SendAsync(meRequest);
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
        var me = await meResponse.Content.ReadFromJsonAsync<AccountResponse>();
        Assert.NotNull(me);
        Assert.Equal(account.AccountId, me.AccountId);
        Assert.Equal("Survivor_01", me.Username);
    }

    [Fact]
    public async Task Register_rejects_duplicate_username_case_insensitively()
    {
        using var client = _factory.CreateClient();
        await RegisterAsync(client, "JabaPlayer", "Password1");

        var response = await client.PostAsJsonAsync(
            ApiRoutes.IdentityRegister,
            new RegisterRequest { Username = "jabaplayer", Password = "Password2" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("username_taken", await ReadCodeAsync(response));
    }

    [Fact]
    public async Task Register_rejects_invalid_password()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            ApiRoutes.IdentityRegister,
            new RegisterRequest { Username = "WeakPassword", Password = "abcdefgh" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("invalid_password", await ReadCodeAsync(response));
    }

    [Fact]
    public async Task Login_rejects_unknown_account_without_disclosing_existence()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            ApiRoutes.IdentityLogin,
            new LoginRequest { Username = "missing", Password = "Password1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("unauthorized", await ReadCodeAsync(response));
    }

    [Fact]
    public async Task Refresh_rotates_token_and_reuse_revokes_the_family()
    {
        using var client = _factory.CreateClient();
        await RegisterAsync(client, "Rotator", "Password1");
        var login = await LoginAsync(client, "Rotator", "Password1");

        var rotated = await RefreshAsync(client, login.RefreshToken);
        Assert.NotEqual(login.RefreshToken, rotated.RefreshToken);
        Assert.Equal(login.AccountId, rotated.AccountId);

        var reuse = await client.PostAsJsonAsync(
            ApiRoutes.IdentityRefresh,
            new RefreshRequest { RefreshToken = login.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);

        var revoked = await client.PostAsJsonAsync(
            ApiRoutes.IdentityRefresh,
            new RefreshRequest { RefreshToken = rotated.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, revoked.StatusCode);
        Assert.Equal("unauthorized", await ReadCodeAsync(revoked));
    }

    [Fact]
    public async Task Logout_revokes_the_active_refresh_session()
    {
        using var client = _factory.CreateClient();
        await RegisterAsync(client, "LogoutUser", "Password1");
        var login = await LoginAsync(client, "LogoutUser", "Password1");

        using var logoutRequest = new HttpRequestMessage(HttpMethod.Post, ApiRoutes.IdentityLogout);
        logoutRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        logoutRequest.Content = JsonContent.Create(new LogoutRequest { RefreshToken = login.RefreshToken });
        var logoutResponse = await client.SendAsync(logoutRequest);
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        var refresh = await client.PostAsJsonAsync(
            ApiRoutes.IdentityRefresh,
            new RefreshRequest { RefreshToken = login.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task Expired_access_token_is_rejected()
    {
        using var client = _factory.CreateClient();
        var account = await RegisterAsync(client, "ExpiredUser", "Password1");

        using var request = new HttpRequestMessage(HttpMethod.Get, ApiRoutes.IdentityMe);
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateAccessToken(account.AccountId, DateTime.UtcNow.AddMinutes(-5)));
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_cannot_revoke_another_accounts_session()
    {
        using var client = _factory.CreateClient();
        await RegisterAsync(client, "FirstUser", "Password1");
        await RegisterAsync(client, "SecondUser", "Password1");
        var first = await LoginAsync(client, "FirstUser", "Password1");
        var second = await LoginAsync(client, "SecondUser", "Password1");

        using var logoutRequest = new HttpRequestMessage(HttpMethod.Post, ApiRoutes.IdentityLogout);
        logoutRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", first.AccessToken);
        logoutRequest.Content = JsonContent.Create(new LogoutRequest { RefreshToken = second.RefreshToken });
        await client.SendAsync(logoutRequest);

        var rotated = await RefreshAsync(client, second.RefreshToken);
        Assert.Equal(second.AccountId, rotated.AccountId);
    }

    [Fact]
    public async Task Me_returns_only_the_authenticated_account()
    {
        using var client = _factory.CreateClient();
        var first = await RegisterAsync(client, "AccountOne", "Password1");
        var second = await RegisterAsync(client, "AccountTwo", "Password1");
        var firstLogin = await LoginAsync(client, "AccountOne", "Password1");

        using var request = new HttpRequestMessage(HttpMethod.Get, ApiRoutes.IdentityMe);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", firstLogin.AccessToken);
        var response = await client.SendAsync(request);
        var me = await response.Content.ReadFromJsonAsync<AccountResponse>();

        Assert.NotNull(me);
        Assert.Equal(first.AccountId, me.AccountId);
        Assert.NotEqual(second.AccountId, me.AccountId);
    }

    private static async Task<AccountResponse> RegisterAsync(HttpClient client, string userName, string password)
    {
        var response = await client.PostAsJsonAsync(
            ApiRoutes.IdentityRegister,
            new RegisterRequest { Username = userName, Password = password });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var account = await response.Content.ReadFromJsonAsync<AccountResponse>();
        Assert.NotNull(account);
        return account;
    }

    private static async Task<TokenResponse> LoginAsync(HttpClient client, string userName, string password)
    {
        var response = await client.PostAsJsonAsync(
            ApiRoutes.IdentityLogin,
            new LoginRequest { Username = userName, Password = password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tokens = await response.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(tokens);
        return tokens;
    }

    private static async Task<TokenResponse> RefreshAsync(HttpClient client, string refreshToken)
    {
        var response = await client.PostAsJsonAsync(
            ApiRoutes.IdentityRefresh,
            new RefreshRequest { RefreshToken = refreshToken });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tokens = await response.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(tokens);
        return tokens;
    }

    private static async Task<string> ReadCodeAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("code").GetString() ?? string.Empty;
    }

    private static long ReadNumericClaim(string token, string claim)
    {
        var payload = token.Split('.')[1];
        using var document = JsonDocument.Parse(Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(payload)));
        return document.RootElement.GetProperty(claim).GetInt64();
    }

    private static string CreateAccessToken(Guid accountId, DateTime expires)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = IdentityApiFactory.Issuer,
            Audience = IdentityApiFactory.Audience,
            IssuedAt = expires.AddMinutes(-15),
            Expires = expires,
            Subject = new ClaimsIdentity(
            [
                new Claim("sub", accountId.ToString()),
                new Claim("username", "ExpiredUser")
            ]),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(IdentityApiFactory.SigningKey)),
                SecurityAlgorithms.HmacSha256)
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}

[Collection(PostgresCollection.Name)]
public sealed class IdentityRateLimitTests(PostgresFixture fixture) : IAsyncLifetime
{
    private IdentityApiFactory _factory = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await fixture.CreateDatabaseAsync("identity_rate");
        _factory = new IdentityApiFactory(connectionString, authPermitLimit: 2).ApplyEnvironment();
        await _factory.InitializeDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Authentication_rate_limit_returns_429_with_retry_after()
    {
        using var client = _factory.CreateClient();
        var payload = new LoginRequest { Username = "rate", Password = "Password1" };

        await client.PostAsJsonAsync(ApiRoutes.IdentityLogin, payload);
        await client.PostAsJsonAsync(ApiRoutes.IdentityLogin, payload);
        var response = await client.PostAsJsonAsync(ApiRoutes.IdentityLogin, payload);

        Assert.Equal((HttpStatusCode)429, response.StatusCode);
        Assert.True(response.Headers.RetryAfter is not null || response.Headers.Contains("Retry-After"));
    }
}
