using System.Globalization;
using System.Threading.RateLimiting;
using Identity.Api.Auth;
using Identity.Api.Data;
using Identity.Api.Endpoints;
using JabaSurvivol.Contracts;
using JabaSurvivol.Security;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var jwtOptions = builder.Services.AddJabaJwtAuthentication(builder.Configuration);
builder.Services.AddSingleton(jwtOptions);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<PasswordService>();
builder.Services.AddScoped<AccessTokenIssuer>();
builder.Services.AddScoped<RefreshTokenService>();

var connectionString = builder.Configuration.GetConnectionString("Database")
    ?? builder.Configuration.GetConnectionString("jabasurvivol")
    ?? throw new InvalidOperationException(
        "A PostgreSQL connection string is required. Set ConnectionStrings:Database.");

builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql =>
        npgsql.MigrationsHistoryTable("__IdentityMigrationsHistory", IdentityDbContext.Schema)));

var authPermitLimit = builder.Configuration.GetValue("RateLimiting:AuthPermitLimit", 20);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("identity-auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = Math.Max(1, authPermitLimit),
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.OnRejected = async (context, cancellationToken) =>
    {
        var response = context.HttpContext.Response;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds)
                .ToString(CultureInfo.InvariantCulture);
        }

        response.StatusCode = StatusCodes.Status429TooManyRequests;
        await ApiProblem.Create(
                StatusCodes.Status429TooManyRequests,
                "rate_limited",
                "Too many requests",
                "Retry after the indicated delay.")
            .ExecuteAsync(context.HttpContext)
            .ConfigureAwait(false);
    };
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    // Identity is reachable only through the BFF, which forwards the client IP.
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();

app.MapGet(ApiRoutes.IdentityStatus, () => Results.Ok(new
{
    service = "identity-api",
    status = "ok",
    version = "v1"
}))
.WithName("GetIdentityStatus");

app.MapIdentityEndpoints();

if (app.Configuration.GetValue("Database:Migrate", app.Environment.IsDevelopment()))
{
    using var scope = app.Services.CreateScope();
    var database = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    database.Database.Migrate();
}

app.Run();

public partial class Program;
