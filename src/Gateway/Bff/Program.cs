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

app.MapGet(ApiRoutes.BffStatus, () => Results.Ok(new
{
    service = "bff",
    status = "ok",
    version = ApiRoutes.Version
}))
.WithName("GetBffStatus");

app.Run();
