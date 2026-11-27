using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Synq.Common.Http;

namespace Synq.Common.Logging;

/// <summary>
/// Struktūrizuoti JSON logai į stdout (P-02, NFR-009).
/// <para>
/// Taisyklės, kad loguose neatsirastų slaptų duomenų: request/response kūnai niekada nelogujami,
/// query string nelogujamas, kelias išvalomas per <see cref="LogSanitizer"/>. Kode loguoti tik id'us –
/// ne slaptažodžius, tokenus, cookie ar žinučių tekstą.
/// </para>
/// </summary>
public static class SerilogSetup
{
    public static WebApplicationBuilder AddSynqLogging(this WebApplicationBuilder builder, string serviceName)
    {
        builder.Services.AddSerilog((services, config) => config
            .ReadFrom.Configuration(builder.Configuration)
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
            .MinimumLevel.Override("Yarp", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.With<RemoveRawPathEnricher>()
            .Enrich.WithProperty("Service", serviceName)
            .WriteTo.Console(new CompactJsonFormatter()));
        return builder;
    }

    /// <summary>
    /// Viena eilutė kiekvienai užklausai: metodas, išvalytas kelias, statusas, trukmė.
    /// Savas middleware vietoj <c>UseSerilogRequestLogging</c>, nes pastarasis visada prideda
    /// neišvalytą <c>RequestPath</c> (jame gali būti kvietimo tokenas).
    /// </summary>
    public static IApplicationBuilder UseSynqRequestLogging(this IApplicationBuilder app) =>
        app.UseMiddleware<RequestLoggingMiddleware>();
}

/// <summary>
/// ASP.NET Core kiekvienam užklausos logui prideda scope lauką <c>RequestPath</c> su neišvalytu keliu
/// (pvz. /api/v1/invitations/&lt;tokenas&gt;). Jį pašaliname – kelias loguojamas tik kaip <c>SafePath</c>.
/// </summary>
internal sealed class RemoveRawPathEnricher : Serilog.Core.ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, Serilog.Core.ILogEventPropertyFactory propertyFactory) =>
        logEvent.RemovePropertyIfPresent("RequestPath");
}

internal sealed class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            await next(context);
            return;
        }

        var started = Stopwatch.GetTimestamp();
        try
        {
            await next(context);
        }
        finally
        {
            var status = context.Response.StatusCode;
            var level = status >= 500 ? LogLevel.Error : LogLevel.Information;
            logger.Log(level, "HTTP {RequestMethod} {SafePath} responded {StatusCode} in {ElapsedMs:0.0} ms",
                context.Request.Method, LogSanitizer.Path(context.Request.Path), status,
                Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }
    }
}
