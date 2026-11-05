FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props global.json ./
COPY src/ src/

RUN dotnet restore src/Services/Telemetry.Worker/Telemetry.Worker.csproj
RUN dotnet publish src/Services/Telemetry.Worker/Telemetry.Worker.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Telemetry.Worker.dll"]
