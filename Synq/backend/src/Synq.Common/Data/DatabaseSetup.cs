using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Synq.Common.Data;

public static class DatabaseSetup
{
    public static IServiceCollection AddSynqDbContext<TDb>(this IServiceCollection services, IConfiguration configuration, string schema)
        where TDb : DbContext
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres is not configured.");
        services.AddDbContext<TDb>(options => Configure(options, connectionString, schema));
        return services;
    }

    public static DbContextOptionsBuilder Configure(DbContextOptionsBuilder options, string connectionString, string schema) =>
        options
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", schema))
            .UseSnakeCaseNamingConvention();

    public static async Task MigrateDatabaseAsync<TDb>(this WebApplication app) where TDb : DbContext
    {
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Synq.Migrations");
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var scope = app.Services.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<TDb>();
                await db.Database.MigrateAsync();
                logger.LogInformation("Database migrations for {DbContext} applied", typeof(TDb).Name);
                return;
            }
            catch (Exception ex) when (attempt < 10 && ex is NpgsqlException or TimeoutException)
            {
                logger.LogWarning("Database not ready (attempt {Attempt}): {Error}", attempt, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }
    }
}
