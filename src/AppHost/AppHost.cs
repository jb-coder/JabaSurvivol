using System.Security.Cryptography;

var builder = DistributedApplication.CreateBuilder(args);

var database = builder.AddPostgres("postgres")
    .AddDatabase("jabasurvivol");

var jwtSigningKey = builder.Configuration["Jwt:SigningKey"]
    ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

var identityApi = builder.AddProject<Projects.Identity_Api>("identity-api")
    .WithReference(database)
    .WithEnvironment("Jwt__SigningKey", jwtSigningKey)
    .WithHttpEndpoint(name: "http");

var gameApi = builder.AddProject<Projects.Game_Api>("game-api")
    .WithReference(database)
    .WithEnvironment("Jwt__SigningKey", jwtSigningKey)
    .WithHttpEndpoint(name: "http");

var telemetryWorker = builder.AddProject<Projects.Telemetry_Worker>("telemetry-worker")
    .WithReference(database)
    .WithEnvironment("Jwt__SigningKey", jwtSigningKey);

builder.AddProject<Projects.Bff>("bff")
    .WithReference(identityApi)
    .WithReference(gameApi)
    .WithReference(telemetryWorker)
    .WithEnvironment("Jwt__SigningKey", jwtSigningKey)
    .WithEnvironment("Upstreams__Identity", identityApi.GetEndpoint("http"))
    .WithEnvironment("Upstreams__Game", gameApi.GetEndpoint("http"))
    .WithExternalHttpEndpoints();

builder.Build().Run();
