using System.Security.Cryptography;

var builder = DistributedApplication.CreateBuilder(args);

var database = builder.AddPostgres("postgres")
    .AddDatabase("jabasurvivol");

var jwtSigningKey = builder.Configuration["Jwt:SigningKey"]
    ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

var identityApi = builder.AddDotnetProject("identity-api", "../Services/Identity.Api/Identity.Api.csproj")
    .WithReference(database)
    .WaitFor(database)
    .WithEnvironment("Jwt__SigningKey", jwtSigningKey)
    .WithHttpEndpoint(name: "http");

var gameApi = builder.AddDotnetProject("game-api", "../Services/Game.Api/Game.Api.csproj")
    .WithReference(database)
    .WaitFor(database)
    .WithEnvironment("Jwt__SigningKey", jwtSigningKey)
    .WithHttpEndpoint(name: "http");

var telemetryWorker = builder.AddDotnetProject("telemetry-worker", "../Services/Telemetry.Worker/Telemetry.Worker.csproj")
    .WithReference(database)
    .WaitFor(database)
    .WithEnvironment("Jwt__SigningKey", jwtSigningKey);

builder.AddDotnetProject("bff", "../Gateway/Bff/Bff.csproj")
    .WithReference(identityApi)
    .WithReference(gameApi)
    .WithReference(telemetryWorker)
    .WaitFor(identityApi)
    .WaitFor(gameApi)
    .WaitFor(telemetryWorker)
    .WithEnvironment("Jwt__SigningKey", jwtSigningKey)
    .WithEnvironment("Upstreams__Identity", identityApi.GetEndpoint("http"))
    .WithEnvironment("Upstreams__Game", gameApi.GetEndpoint("http"))
    .WithExternalHttpEndpoints();

builder.Build().Run();
