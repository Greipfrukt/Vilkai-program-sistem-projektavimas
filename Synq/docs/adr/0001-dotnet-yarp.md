# ADR 0001 – Backend: .NET 8 + YARP

**Būsena:** priimta (2026-11-22)

## Kontekstas
Reikia 7 mikroservisų ir API gateway, kuriuos 3 žmonės parašytų per 3 savaites ir kurie veiktų per Docker Compose (taip pat Mac arm64).

## Sprendimas
- C# / ASP.NET Core **.NET 8** (ne .NET 10 – komanda jį moka), EF Core 8 + Npgsql, xUnit + Testcontainers.
- Gateway – **YARP** (ASP.NET Core reverse proxy): maršrutai konfigūruojami `appsettings.json` pagal `Synq.yaml` „Gateway routing“.
- Bendras kodas – `Synq.Common` biblioteka; paketų versijos – `Directory.Packages.props`.

## Pasekmės
- .NET 8 palaikymas baigėsi 2026-11-10 – lokaliam universitetiniam projektui tai priimtina, tikram produktui reikėtų .NET 10 LTS.
- Gateway ir servisai – viena technologija, vienas klaidų/logų formatas.
