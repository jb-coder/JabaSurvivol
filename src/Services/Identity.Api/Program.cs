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

app.MapGet(ApiRoutes.IdentityStatus, () => Results.Ok(new
{
    service = "identity-api",
    status = "ok",
    version = ApiRoutes.Version
}))
.WithName("GetIdentityStatus");

app.Run();
