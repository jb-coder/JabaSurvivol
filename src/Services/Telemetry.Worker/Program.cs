using Microsoft.AspNetCore.Builder;
using Game.Data;
using Microsoft.EntityFrameworkCore;
using Telemetry.Worker;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.Configure<TelemetryWorkerOptions>(
    builder.Configuration.GetSection(TelemetryWorkerOptions.SectionName));
builder.Services.AddSingleton(TimeProvider.System);

var connectionString = builder.Configuration.GetConnectionString("Database")
    ?? builder.Configuration.GetConnectionString("jabasurvivol")
    ?? throw new InvalidOperationException(
        "A PostgreSQL connection string is required. Set ConnectionStrings:Database.");

builder.Services.AddDbContext<TelemetryDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql =>
        npgsql.MigrationsHistoryTable("__TelemetryMigrationsHistory", TelemetryDbContext.Schema)));
builder.Services.AddScoped<TelemetryAggregationService>();
builder.Services.AddHostedService<TelemetryWorker>();

var app = builder.Build();
app.MapDefaultEndpoints();
app.Run();
