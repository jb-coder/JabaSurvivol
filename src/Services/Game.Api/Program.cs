using JabaSurvivol.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapDefaultEndpoints();

app.MapGet(ApiRoutes.GameStatus, () => Results.Ok(new
{
    service = "game-api",
    status = "ok",
    version = ApiRoutes.Version
}))
.WithName("GetGameStatus");

app.Run();
