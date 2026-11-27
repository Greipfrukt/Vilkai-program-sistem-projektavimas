using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Synq.Common.Errors;
using Synq.Common.Health;
using Synq.Common.Http;
using Synq.Common.Json;
using Synq.Common.Logging;

namespace Synq.Common;

public static class SynqDefaults
{
    public static WebApplicationBuilder AddSynqDefaults(this WebApplicationBuilder builder, string serviceName)
    {
        builder.AddSynqLogging(serviceName);
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.ConfigureHttpJsonOptions(o => SynqJson.Configure(o.SerializerOptions));
        builder.Services.Configure<RouteHandlerOptions>(o => o.ThrowOnBadRequest = true);
        builder.Services.AddSynqHealth();
        return builder;
    }

    public static WebApplication UseSynqDefaults(this WebApplication app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseSynqRequestLogging();
        app.UseMiddleware<ExceptionMiddleware>();
        app.UseStatusCodePages(WriteEmptyErrorAsync);
        app.Use((context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                if (!context.Response.Headers.ContainsKey("Cache-Control"))
                    context.Response.Headers.CacheControl = "no-store";
                return Task.CompletedTask;
            });
            return next(context);
        });
        app.MapSynqHealth();
        return app;
    }

    private static Task WriteEmptyErrorAsync(StatusCodeContext statusContext)
    {
        var context = statusContext.HttpContext;
        var code = ErrorCodes.ForStatus(context.Response.StatusCode);
        if (ErrorCodes.StatusOf(code) != context.Response.StatusCode)
            return Task.CompletedTask;
        return ExceptionMiddleware.WriteAsync(context, code, ErrorCodes.MessageOf(code));
    }
}
