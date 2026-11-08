using Game.Api.Saves;
using Game.Data;
using JabaSurvivol.Contracts;
using JabaSurvivol.Security;
using Microsoft.EntityFrameworkCore;

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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseAuthentication();
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

if (app.Configuration.GetValue("Database:Migrate", app.Environment.IsDevelopment()))
{
    using var scope = app.Services.CreateScope();
    var database = scope.ServiceProvider.GetRequiredService<GameDbContext>();
    database.Database.Migrate();
}

app.Run();

/// <summary>
/// Marker type used by integration tests to locate the Game API assembly.
/// </summary>
public sealed class GameApiMarker;
