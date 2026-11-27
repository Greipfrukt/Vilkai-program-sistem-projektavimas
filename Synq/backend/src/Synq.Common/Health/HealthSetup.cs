using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using StackExchange.Redis;
using Synq.Common.Events;
using Synq.Common.Json;

namespace Synq.Common.Health;

/// <summary>
/// /health/live ir /health/ready pagal yaml schemą <c>Health</c> (NFR-015, P-02):
/// <c>{"status":"UP","checks":{"postgres":"UP","rabbitmq":"UP","redis":"UP"}}</c>.
/// <list type="bullet">
/// <item><b>live</b> – 200, kol procesas veikia (be priklausomybių – kitaip Docker perkrautų sveiką procesą dėl DB);</item>
/// <item><b>ready</b> – 200 tik kai visos serviso priklausomybės pasiekiamos, kitaip 503. Naudoja compose ir gateway.</item>
/// </list>
/// </summary>
public static class HealthSetup
{
    public const string ReadyTag = "ready";
    public const string Postgres = "postgres";
    public const string RabbitMq = "rabbitmq";
    public const string Redis = "redis";

    private static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(2);

    public static IHealthChecksBuilder AddSynqHealth(this IServiceCollection services) =>
        services.AddHealthChecks();

    public static IHealthChecksBuilder AddPostgresCheck(this IHealthChecksBuilder builder, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres");
        return builder.AddAsyncCheck(Postgres, async ct =>
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(ct);
            await using var command = new NpgsqlCommand("SELECT 1", connection);
            await command.ExecuteScalarAsync(ct);
            return HealthCheckResult.Healthy();
        }, [ReadyTag], CheckTimeout);
    }

    public static IHealthChecksBuilder AddRabbitMqCheck(this IHealthChecksBuilder builder) =>
        builder.Add(new HealthCheckRegistration(RabbitMq, sp => new DelegateHealthCheck(async ct =>
        {
            var connection = await sp.GetRequiredService<RabbitMqConnection>().GetAsync(ct);
            return connection.IsOpen ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("Connection is recovering");
        }), HealthStatus.Unhealthy, [ReadyTag], CheckTimeout));

    public static IHealthChecksBuilder AddRedisCheck(this IHealthChecksBuilder builder) =>
        builder.Add(new HealthCheckRegistration(Redis, sp => new DelegateHealthCheck(async _ =>
        {
            await sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase().PingAsync();
            return HealthCheckResult.Healthy();
        }), HealthStatus.Unhealthy, [ReadyTag], CheckTimeout));

    public static IEndpointRouteBuilder MapSynqHealth(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = WriteAsync,
        });
        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(ReadyTag),
            ResponseWriter = WriteAsync,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
            },
        });
        return endpoints;
    }

    private static Task WriteAsync(HttpContext context, HealthReport report)
    {
        var body = new
        {
            status = report.Status == HealthStatus.Unhealthy ? "DOWN" : "UP",
            checks = report.Entries.ToDictionary(e => e.Key, e => e.Value.Status == HealthStatus.Unhealthy ? "DOWN" : "UP"),
        };
        context.Response.ContentType = "application/json";
        context.Response.Headers.CacheControl = "no-store";
        return context.Response.WriteAsync(JsonSerializer.Serialize(body, SynqJson.Options));
    }

    private sealed class DelegateHealthCheck(Func<CancellationToken, Task<HealthCheckResult>> check) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                return await check(cancellationToken);
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(ex.Message);
            }
        }
    }
}
