using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace Synq.TestKit;

/// <summary>
/// Tikras Postgres ir Redis Docker konteineriuose (Testcontainers) – vienas visam testų rinkiniui.
/// Integraciniai testai tikrina tikrą SQL (unikalumo indeksus, transakcijas, ILIKE), ne in-memory imitaciją.
/// Testai vienas kito netrukdo, nes kiekvienas kuria savo vartotojus / workspace'us su unikaliais duomenimis.
/// </summary>
public sealed class InfrastructureFixture : IAsyncLifetime
{
    public PostgreSqlContainer Postgres { get; } = new PostgreSqlBuilder("postgres:16").Build();
    public RedisContainer Redis { get; } = new RedisBuilder("redis:7").Build();

    public Task InitializeAsync() => Task.WhenAll(Postgres.StartAsync(), Redis.StartAsync());

    public async Task DisposeAsync()
    {
        await Postgres.DisposeAsync();
        await Redis.DisposeAsync();
    }
}

/// <summary>
/// xUnit reikalauja, kad collection apibrėžimas būtų pačiame testų projekte, todėl kiekvienas projektas turi:
/// <code>[CollectionDefinition(InfrastructureCollection.Name)] public sealed class Infra : ICollectionFixture&lt;InfrastructureFixture&gt;;</code>
/// </summary>
public static class InfrastructureCollection
{
    public const string Name = "infrastructure";
}
