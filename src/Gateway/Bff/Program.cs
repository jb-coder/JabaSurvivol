using Bff;
using JabaSurvivol.Contracts;
using JabaSurvivol.Security;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddJabaJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization(options =>
    options.AddPolicy(ReverseProxyConfiguration.AuthenticatedPolicy, policy =>
        policy.RequireAuthenticatedUser()));
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    // The BFF is reachable only through the cluster ingress, which sets the client IP.
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.AddSingleton<BffRateLimiter>();

builder.Services
    .AddReverseProxy()
    .LoadFromMemory(
        ReverseProxyConfiguration.CreateRoutes(),
        ReverseProxyConfiguration.CreateClusters(builder.Configuration));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseAuthentication();
app.UseMiddleware<BffRateLimitMiddleware>();
app.UseAuthorization();

app.MapDefaultEndpoints();

app.MapGet(ApiRoutes.BffStatus, () => Results.Ok(new
{
    service = "bff",
    status = "ok",
    version = ApiRoutes.Version
}))
.AllowAnonymous()
.WithName("GetBffStatus");

app.UseMiddleware<ProxyErrorMiddleware>();
app.MapReverseProxy();

app.Run();
