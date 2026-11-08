FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/Services/Telemetry.Worker/Telemetry.Worker.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
USER $APP_UID
HEALTHCHECK --interval=30s --timeout=3s --start-period=20s --retries=3 \
    CMD test -r /proc/1 || exit 1
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Telemetry.Worker.dll"]
