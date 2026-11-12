FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/Services/Game.Api/Game.Api.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN apt-get update \
    && apt-get upgrade -y \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
USER $APP_UID
HEALTHCHECK --interval=30s --timeout=3s --start-period=20s --retries=3 \
    CMD curl -fsS http://127.0.0.1:8080/alive || exit 1
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Game.Api.dll"]
