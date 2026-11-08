using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bff;
using JabaSurvivol.Contracts;

namespace JabaSurvivol.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class BffRoutingTests
{
    [Fact]
    public async Task Anonymous_config_route_is_forwarded_to_game()
    {
        using var factory = new BffFactory().ApplyEnvironment();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(ApiRoutes.GameConfig);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var request = Assert.Single(factory.Forwarder.Requests);
        Assert.Equal("game-api", request.Host);
        Assert.Equal(ApiRoutes.GameConfig, request.Path);
        Assert.Equal("GET", request.Method);
    }

    [Fact]
    public async Task Protected_route_without_token_returns_problem_details_and_is_not_forwarded()
    {
        using var factory = new BffFactory().ApplyEnvironment();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(ApiRoutes.CloudSavePrimary);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("unauthorized", await ReadCodeAsync(response));
        Assert.Empty(factory.Forwarder.Requests);
    }

    [Fact]
    public async Task Protected_route_with_token_is_forwarded_with_authorization()
    {
        using var factory = new BffFactory().ApplyEnvironment();
        using var client = factory.CreateAuthenticatedClient(Guid.NewGuid());

        var response = await client.GetAsync(ApiRoutes.CloudSavePrimary);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var request = Assert.Single(factory.Forwarder.Requests);
        Assert.Equal("game-api", request.Host);
        Assert.Equal(ApiRoutes.CloudSavePrimary, request.Path);
        Assert.False(string.IsNullOrWhiteSpace(request.Authorization));
    }

    [Fact]
    public async Task Identity_routes_are_forwarded_to_the_identity_upstream()
    {
        using var factory = new BffFactory().ApplyEnvironment();
        using var client = factory.CreateAuthenticatedClient(Guid.NewGuid());

        var response = await client.GetAsync(ApiRoutes.IdentityMe);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var request = Assert.Single(factory.Forwarder.Requests);
        Assert.Equal("identity-api", request.Host);
        Assert.Equal(ApiRoutes.IdentityMe, request.Path);
    }

    [Fact]
    public async Task Internal_routes_that_are_not_public_are_not_proxied()
    {
        using var factory = new BffFactory().ApplyEnvironment();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/game/status");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(factory.Forwarder.Requests);
    }

    [Fact]
    public async Task Correlation_id_is_generated_and_propagated()
    {
        using var factory = new BffFactory().ApplyEnvironment();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(ApiRoutes.GameConfig);

        Assert.True(response.Headers.TryGetValues(CorrelationIdMiddleware.HeaderName, out var values));
        var correlationId = Assert.Single(values);
        var request = Assert.Single(factory.Forwarder.Requests);
        Assert.Equal(correlationId, request.CorrelationId);
    }

    [Fact]
    public async Task Correlation_id_is_preserved_when_supplied()
    {
        using var factory = new BffFactory().ApplyEnvironment();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, ApiRoutes.GameConfig);
        request.Headers.Add(CorrelationIdMiddleware.HeaderName, "correlation-123");

        var response = await client.SendAsync(request);

        Assert.Equal("correlation-123", response.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single());
        Assert.Equal("correlation-123", Assert.Single(factory.Forwarder.Requests).CorrelationId);
    }

    [Fact]
    public async Task Network_failure_maps_to_safe_problem_details()
    {
        using var factory = new BffFactory().ApplyEnvironment();
        factory.Forwarder.ExceptionToThrow = new HttpRequestException("internal failure detail");
        using var client = factory.CreateClient();

        var response = await client.GetAsync(ApiRoutes.GameConfig);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal("upstream_unavailable", await ReadCodeAsync(response));
        Assert.DoesNotContain("internal failure detail", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Timeout_maps_to_gateway_timeout_problem_details()
    {
        using var factory = new BffFactory().ApplyEnvironment();
        factory.Forwarder.ExceptionToThrow = new TaskCanceledException("timeout detail");
        using var client = factory.CreateClient();

        var response = await client.GetAsync(ApiRoutes.GameConfig);

        Assert.Equal(HttpStatusCode.GatewayTimeout, response.StatusCode);
        Assert.Equal("upstream_timeout", await ReadCodeAsync(response));
        Assert.DoesNotContain("timeout detail", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Invalid_access_token_returns_problem_details()
    {
        using var factory = new BffFactory().ApplyEnvironment();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, ApiRoutes.CloudSavePrimary);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid-token");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("unauthorized", await ReadCodeAsync(response));
        Assert.Empty(factory.Forwarder.Requests);
    }

    [Theory]
    [InlineData("POST", "/api/v1/identity/register", "identity-api", false)]
    [InlineData("POST", "/api/v1/identity/login", "identity-api", false)]
    [InlineData("POST", "/api/v1/identity/refresh", "identity-api", false)]
    [InlineData("POST", "/api/v1/identity/logout", "identity-api", true)]
    [InlineData("GET", "/api/v1/identity/me", "identity-api", true)]
    [InlineData("GET", "/api/v1/game/config", "game-api", false)]
    [InlineData("GET", "/api/v1/game/saves/primary", "game-api", true)]
    [InlineData("PUT", "/api/v1/game/saves/primary", "game-api", true)]
    [InlineData("POST", "/api/v1/game/leaderboards/highest-round/runs", "game-api", true)]
    [InlineData("GET", "/api/v1/game/leaderboards/highest-round", "game-api", false)]
    [InlineData("POST", "/api/v1/telemetry/events", "game-api", true)]
    public async Task Documented_routes_forward_to_the_expected_upstream(
        string method,
        string path,
        string host,
        bool authenticated)
    {
        using var factory = new BffFactory().ApplyEnvironment();
        using var client = authenticated
            ? factory.CreateAuthenticatedClient(Guid.NewGuid())
            : factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method is "POST" or "PUT")
        {
            request.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        }

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var recorded = Assert.Single(factory.Forwarder.Requests);
        Assert.Equal(host, recorded.Host);
        Assert.Equal(path, recorded.Path);
        Assert.Equal(method, recorded.Method);
    }

    [Theory]
    [InlineData("GET", "/api/v1/game/saves/other")]
    [InlineData("POST", "/api/v1/telemetry/other")]
    public async Task Undocumented_subpaths_are_not_forwarded(string method, string path)
    {
        using var factory = new BffFactory().ApplyEnvironment();
        using var client = factory.CreateAuthenticatedClient(Guid.NewGuid());
        using var request = new HttpRequestMessage(new HttpMethod(method), path);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(factory.Forwarder.Requests);
    }

    [Theory]
    [InlineData("DELETE", "/api/v1/game/saves/primary")]
    [InlineData("GET", "/api/v1/identity/logout")]
    [InlineData("GET", "/api/v1/telemetry/events")]
    public async Task Undocumented_methods_on_protected_routes_are_not_forwarded(string method, string path)
    {
        using var factory = new BffFactory().ApplyEnvironment();
        using var client = factory.CreateAuthenticatedClient(Guid.NewGuid());
        using var request = new HttpRequestMessage(new HttpMethod(method), path);

        var response = await client.SendAsync(request);

        Assert.True(
            response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed,
            $"Expected 404 or 405, got {(int)response.StatusCode}.");
        Assert.Empty(factory.Forwarder.Requests);
    }

    [Fact]
    public async Task Per_account_rate_limit_returns_429()
    {
        using var factory = new BffFactory(accountPermitLimit: 2).ApplyEnvironment();
        using var client = factory.CreateAuthenticatedClient(Guid.NewGuid());
        var payload = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");

        Assert.Equal(HttpStatusCode.OK, (await client.PutAsync(ApiRoutes.CloudSavePrimary, payload)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsync(ApiRoutes.CloudSavePrimary, new StringContent("{}", System.Text.Encoding.UTF8, "application/json"))).StatusCode);
        var response = await client.PutAsync(ApiRoutes.CloudSavePrimary, new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal((HttpStatusCode)429, response.StatusCode);
        Assert.Equal("rate_limited", await ReadCodeAsync(response));
    }

    [Fact]
    public async Task Per_ip_config_rate_limit_returns_429()
    {
        using var factory = new BffFactory(ipPermitLimit: 2).ApplyEnvironment();
        using var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(ApiRoutes.GameConfig)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(ApiRoutes.GameConfig)).StatusCode);
        var response = await client.GetAsync(ApiRoutes.GameConfig);

        Assert.Equal((HttpStatusCode)429, response.StatusCode);
    }

    [Fact]
    public async Task Rate_limit_policy_matching_is_case_insensitive()
    {
        using var factory = new BffFactory(ipPermitLimit: 1).ApplyEnvironment();
        using var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/API/v1/game/config")).StatusCode);
        var response = await client.GetAsync("/API/v1/game/config");

        Assert.Equal((HttpStatusCode)429, response.StatusCode);
    }

    [Fact]
    public async Task Forwarded_client_ip_partitions_the_rate_limit()
    {
        using var factory = new BffFactory(ipPermitLimit: 1).ApplyEnvironment();
        using var client = factory.CreateClient();

        using var first = new HttpRequestMessage(HttpMethod.Get, ApiRoutes.GameConfig);
        first.Headers.Add("X-Forwarded-For", "203.0.113.10");
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(first)).StatusCode);

        using var second = new HttpRequestMessage(HttpMethod.Get, ApiRoutes.GameConfig);
        second.Headers.Add("X-Forwarded-For", "203.0.113.11");
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(second)).StatusCode);

        using var third = new HttpRequestMessage(HttpMethod.Get, ApiRoutes.GameConfig);
        third.Headers.Add("X-Forwarded-For", "203.0.113.10");
        Assert.Equal((HttpStatusCode)429, (await client.SendAsync(third)).StatusCode);
    }

    [Fact]
    public async Task Me_uses_the_per_account_rate_limit()
    {
        using var factory = new BffFactory(accountPermitLimit: 1).ApplyEnvironment();
        var firstAccount = Guid.NewGuid();
        using var first = factory.CreateAuthenticatedClient(firstAccount);

        Assert.Equal(HttpStatusCode.OK, (await first.GetAsync(ApiRoutes.IdentityMe)).StatusCode);
        Assert.Equal((HttpStatusCode)429, (await first.GetAsync(ApiRoutes.IdentityMe)).StatusCode);

        using var second = factory.CreateAuthenticatedClient(Guid.NewGuid());
        Assert.Equal(HttpStatusCode.OK, (await second.GetAsync(ApiRoutes.IdentityMe)).StatusCode);
    }
    private static async Task<string> ReadCodeAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("code").GetString() ?? string.Empty;
    }
}

[Collection(PostgresCollection.Name)]
public sealed class BffRateLimitTests
{
    [Fact]
    public async Task Authentication_rate_limit_returns_429_with_retry_after()
    {
        using var factory = new BffFactory(authPermitLimit: 2).ApplyEnvironment();
        using var client = factory.CreateClient();
        var payload = JsonContent.Create(new { username = "rate", password = "Password1" });

        await client.PostAsync(ApiRoutes.IdentityLogin, payload);
        await client.PostAsync(ApiRoutes.IdentityLogin, JsonContent.Create(new { username = "rate", password = "Password1" }));
        var response = await client.PostAsync(ApiRoutes.IdentityLogin, JsonContent.Create(new { username = "rate", password = "Password1" }));

        Assert.Equal((HttpStatusCode)429, response.StatusCode);
        Assert.True(response.Headers.RetryAfter is not null || response.Headers.Contains("Retry-After"));
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("rate_limited", problem.RootElement.GetProperty("code").GetString());
    }
}
