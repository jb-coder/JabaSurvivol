using System.Globalization;
using System.Threading.RateLimiting;
using Game.Api.Configuration;
using Game.Api.Leaderboards;
using Game.Api.Saves;
using Game.Api.Telemetry;
using Game.Data;
using JabaSurvivol.Contracts;
using JabaSurvivol.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddJabaJwtAuthentication(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);

var connectionString = builder.Configuration.GetConnectionString("Database")
    ?? builder.Configuration.GetConnectionString("jabasurvivol")
    ?? throw new InvalidOperationException(
        "A PostgreSQL connection string is required. Set ConnectionStrings:Database.");

builder.Services.AddDbContext<GameDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql =>
        npgsql.MigrationsHistoryTable("__GameMigrationsHistory", GameDbContext.Schema)));

builder.Services.AddDbContext<TelemetryDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql =>
        npgsql.MigrationsHistoryTable("__TelemetryMigrationsHistory", TelemetryDbContext.Schema)));

var leaderboardPermitLimit = builder.Configuration.GetValue("RateLimiting:LeaderboardSubmitPermitLimit", 30);
builder.Services.AddOptions<RemoteConfigOptions>()
    .Bind(builder.Configuration.GetSection(RemoteConfigOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<RemoteConfigOptions>, RemoteConfigOptionsValidator>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("leaderboard-submit", context =>
    {
        var partitionKey = context.User.TryGetAccountId(out var accountId)
            ? accountId.ToString("N", CultureInfo.InvariantCulture)
            : context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = Math.Max(1, leaderboardPermitLimit),
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });

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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapDefaultEndpoints();

app.MapGet(ApiRoutes.GameStatus, () => Results.Ok(new
{
    service = "game-api",
    status = "ok",
    version = ApiRoutes.Version
}))
.WithName("GetGameStatus");

app.MapSaveEndpoints();
app.MapRemoteConfigEndpoint();
app.MapTelemetryEndpoints();
app.MapLeaderboardEndpoints();

if (app.Configuration.GetValue("Database:Migrate", app.Environment.IsDevelopment()))
{
    using var scope = app.Services.CreateScope();
    var database = scope.ServiceProvider.GetRequiredService<GameDbContext>();
    database.Database.Migrate();

    var telemetry = scope.ServiceProvider.GetRequiredService<TelemetryDbContext>();
    telemetry.Database.Migrate();
}

app.Run();

/// <summary>
/// Marker type used by integration tests to locate the Game API assembly.
/// </summary>
public sealed class GameApiMarker;
