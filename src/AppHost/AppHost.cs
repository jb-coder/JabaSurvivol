var builder = DistributedApplication.CreateBuilder(args);

var identityApi = builder.AddProject<Projects.Identity_Api>("identity-api");
var gameApi = builder.AddProject<Projects.Game_Api>("game-api");
var telemetryWorker = builder.AddProject<Projects.Telemetry_Worker>("telemetry-worker");

builder.AddProject<Projects.Bff>("bff")
    .WithReference(identityApi)
    .WithReference(gameApi)
    .WithReference(telemetryWorker)
    .WithExternalHttpEndpoints();

builder.Build().Run();
