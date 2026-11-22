# Build context: backend/  (žr. docker-compose.yml)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/Synq.Common/Synq.Common.csproj src/Synq.Common/
COPY src/Synq.Presence/Synq.Presence.csproj src/Synq.Presence/
RUN dotnet restore src/Synq.Presence/Synq.Presence.csproj
COPY src/Synq.Common/ src/Synq.Common/
COPY src/Synq.Presence/ src/Synq.Presence/
RUN dotnet publish src/Synq.Presence/Synq.Presence.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Synq.Presence.dll"]
