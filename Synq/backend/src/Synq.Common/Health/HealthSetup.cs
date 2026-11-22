using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Synq.Common.Health;

/// <summary>
/// /health/live ir /health/ready (NFR-015). P-01 skeletas: kol kas be priklausomybių patikrų –
/// P-02 prideda postgres / rabbitmq / redis patikras pagal yaml schemą <c>Health</c>.
/// </summary>
public static class HealthSetup
{
    public const string ReadyTag = "ready";

    public static IHealthChecksBuilder AddSynqHealth(this IServiceCollection services) =>
        services.AddHealthChecks();

    public static IEndpointRouteBuilder MapSynqHealth(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(ReadyTag),
        });
        return endpoints;
    }
}
