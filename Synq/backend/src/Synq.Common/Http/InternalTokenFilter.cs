using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Synq.Common.Errors;

namespace Synq.Common.Http;

/// <summary>
/// X-Internal-Token tikrinimas /internal/** endpoint'ams (P-02). Tokenas – bendra paslaptis iš
/// konfigūracijos <c>Internal:Token</c> (compose: INTERNAL_TOKEN). Lyginama pastoviu laiku.
/// </summary>
public sealed class InternalTokenFilter(IConfiguration configuration) : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var expected = configuration["Internal:Token"];
        var actual = context.HttpContext.Request.Headers[SynqHeaders.InternalToken].ToString();

        if (string.IsNullOrEmpty(expected) || !FixedTimeEquals(expected, actual))
            throw ApiException.AuthRequired();

        return next(context);
    }

    private static bool FixedTimeEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
}

public static class InternalTokenFilterExtensions
{
    /// <example>app.MapGroup("/internal/v1").RequireInternalToken();</example>
    public static TBuilder RequireInternalToken<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder =>
        builder.AddEndpointFilter<TBuilder, InternalTokenFilter>();
}

/// <summary>
/// HttpClient handler'is vidiniams kvietimams: prideda X-Internal-Token ir perduoda X-Correlation-Id.
/// Registruojama per <see cref="InternalHttpClientExtensions.AddSynqInternalClient"/>.
/// </summary>
public sealed class InternalCallHandler(IConfiguration configuration) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.TryAddWithoutValidation(SynqHeaders.InternalToken, configuration["Internal:Token"]);
        if (CorrelationContext.Current is { } correlationId)
            request.Headers.TryAddWithoutValidation(SynqHeaders.CorrelationId, correlationId.ToString());
        return base.SendAsync(request, cancellationToken);
    }
}

public static class InternalHttpClientExtensions
{
    /// <summary>
    /// Vardinis HttpClient kitam servisui. Laiko riba 2 s (yaml „Error handling between services“).
    /// </summary>
    /// <example>services.AddSynqInternalClient("auth", "http://auth:8080");</example>
    public static IHttpClientBuilder AddSynqInternalClient(this IServiceCollection services, string name, string baseAddress)
    {
        services.AddTransient<InternalCallHandler>();
        return services.AddHttpClient(name, client =>
            {
                client.BaseAddress = new Uri(baseAddress);
                client.Timeout = TimeSpan.FromSeconds(2);
            })
            .AddHttpMessageHandler<InternalCallHandler>();
    }
}
