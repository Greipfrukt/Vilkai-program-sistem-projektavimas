# ADR 0001 – Backend: .NET 10 + YARP

**Būsena:** priimta (2026-11-23; pakeičia 2026-11-22 sprendimą naudoti .NET 8)

## Kontekstas
Reikia 7 mikroservisų ir API gateway, kuriuos 3 žmonės parašytų per 3 savaites ir kurie veiktų per Docker Compose (taip pat Mac arm64).

## Sprendimas
- C# / ASP.NET Core **.NET 10 (LTS)**, EF Core 10 + Npgsql, xUnit + Testcontainers.
- Gateway – **YARP** (ASP.NET Core reverse proxy): maršrutai konfigūruojami `appsettings.json` pagal `Synq.yaml` „Gateway routing“.
- Bendras kodas – `Synq.Common` biblioteka; paketų versijos – `Directory.Packages.props`.
- SDK versija užfiksuota `global.json` (10.0.x), Docker image'ai – `dotnet/sdk:10.0` ir `dotnet/aspnet:10.0` (turi arm64).

## Pasekmės
- .NET 8 palaikymas baigėsi 2026-11-10, todėl pasirinkta palaikoma LTS versija (palaikymas iki 2028-11) – atitinka darbų plano 1 skyrių.
- Visiems (ir Augustino Mac'ui) reikia .NET 10 SDK.
- Gateway ir servisai – viena technologija, vienas klaidų/logų formatas.
