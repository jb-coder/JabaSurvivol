using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Bff;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Yarp.ReverseProxy.Forwarder;

namespace JabaSurvivol.IntegrationTests;

public sealed class BffFactory(
    int authPermitLimit = 1_000,
    int accountPermitLimit = 1_000,
    int ipPermitLimit = 1_000) : WebApplicationFactory<BffMarker>
{
    public const string Issuer = "JabaSurvivol.Tests";
    public const string Audience = "JabaSurvivol.Tests";

    public string SigningKey { get; } = "jaba-bff-" + Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
    public FakeForwarderFactory Forwarder { get; } = new();

    public BffFactory ApplyEnvironment()
    {
        Environment.SetEnvironmentVariable("Jwt__SigningKey", SigningKey);
        Environment.SetEnvironmentVariable("Jwt__Issuer", Issuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", Audience);
        Environment.SetEnvironmentVariable("Upstreams__Identity", "http://identity-api");
        Environment.SetEnvironmentVariable("Upstreams__Game", "http://game-api");
        Environment.SetEnvironmentVariable("RateLimiting__AuthPermitLimit", authPermitLimit.ToString());
        Environment.SetEnvironmentVariable("RateLimiting__AccountPermitLimit", accountPermitLimit.ToString());
        Environment.SetEnvironmentVariable("RateLimiting__IpPermitLimit", ipPermitLimit.ToString());
        return this;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = SigningKey,
                ["Jwt:Issuer"] = Issuer,
                ["Jwt:Audience"] = Audience,
                ["Upstreams:Identity"] = "http://identity-api",
                ["Upstreams:Game"] = "http://game-api",
                ["RateLimiting:AuthPermitLimit"] = authPermitLimit.ToString(),
                ["RateLimiting:AccountPermitLimit"] = accountPermitLimit.ToString(),
                ["RateLimiting:IpPermitLimit"] = ipPermitLimit.ToString()
            });
        });
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IForwarderHttpClientFactory>();
            services.AddSingleton<IForwarderHttpClientFactory>(Forwarder);
        });
    }

    public HttpClient CreateAuthenticatedClient(Guid accountId)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                TestTokenFactory.Create(accountId, SigningKey, Issuer, Audience));
        return client;
    }
}

public sealed class FakeForwarderFactory : IForwarderHttpClientFactory
{
    private readonly ConcurrentQueue<RecordedRequest> _requests = new();
    private readonly object _sync = new();

    public Exception? ExceptionToThrow { get; set; }
    public Func<RecordedRequest, HttpResponseMessage>? Responder { get; set; }

    public IReadOnlyList<RecordedRequest> Requests => _requests.ToArray();

    public HttpMessageInvoker CreateClient(ForwarderHttpClientContext context)
    {
        return new HttpMessageInvoker(new FakeHandler(this));
    }

    private void Record(RecordedRequest request)
    {
        lock (_sync)
        {
            _requests.Enqueue(request);
        }
    }

    private sealed class FakeHandler(FakeForwarderFactory owner) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var recorded = new RecordedRequest(
                request.Method.Method,
                request.RequestUri?.AbsolutePath ?? string.Empty,
                request.RequestUri?.Host ?? string.Empty,
                request.Headers.TryGetValues("X-Correlation-ID", out var correlation) ? correlation.FirstOrDefault() : null,
                request.Headers.Authorization?.Parameter);

            owner.Record(recorded);

            if (owner.ExceptionToThrow is not null)
            {
                throw owner.ExceptionToThrow;
            }

            if (owner.Responder is not null)
            {
                return Task.FromResult(owner.Responder(recorded));
            }

            var body = $"{{\"upstream\":\"{recorded.Host}\",\"path\":\"{recorded.Path}\"}}";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }
}

public sealed record RecordedRequest(
    string Method,
    string Path,
    string Host,
    string? CorrelationId,
    string? Authorization);
