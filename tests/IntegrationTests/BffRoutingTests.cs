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
