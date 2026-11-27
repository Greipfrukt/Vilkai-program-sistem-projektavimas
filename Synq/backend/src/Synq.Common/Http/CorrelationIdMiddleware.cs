using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Synq.Common.Http;

/// <summary>
/// Dabartinės operacijos correlation id. <see cref="AsyncLocal{T}"/> leidžia jį pasiekti ir ten,
/// kur nėra HttpContext (outbox rašymas, įvykių vartotojai, vidiniai HTTP kvietimai).
/// </summary>
public static class CorrelationContext
{
    private static readonly AsyncLocal<Guid?> CurrentId = new();

    public static Guid? Current
    {
        get => CurrentId.Value;
        set => CurrentId.Value = value;
    }

    /// <summary>Nustato id bloko trukmei ir prideda jį prie visų tame bloke rašomų logų.</summary>
    public static IDisposable Begin(Guid correlationId)
    {
        var previous = CurrentId.Value;
        CurrentId.Value = correlationId;
        var logScope = LogContext.PushProperty("CorrelationId", correlationId);
        return new Scope(() =>
        {
            logScope.Dispose();
            CurrentId.Value = previous;
        });
    }

    private sealed class Scope(Action onDispose) : IDisposable
    {
        public void Dispose() => onDispose();
    }
}

/// <summary>
/// X-Correlation-Id (P-02): paima iš užklausos (gateway jį nustato) arba sugeneruoja naują,
/// grąžina kiekviename atsakyme ir palieka užklausos antraštėse, kad YARP perduotų jį toliau.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string ItemKey = "Synq.CorrelationId";

    public async Task InvokeAsync(HttpContext context)
    {
        var id = Guid.TryParse(context.Request.Headers[SynqHeaders.CorrelationId], out var parsed) && parsed != Guid.Empty
            ? parsed
            : Guid.NewGuid();

        context.Items[ItemKey] = id;
        context.Request.Headers[SynqHeaders.CorrelationId] = id.ToString();
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[SynqHeaders.CorrelationId] = id.ToString();
            return Task.CompletedTask;
        });

        using (CorrelationContext.Begin(id))
            await next(context);
    }

    internal static Guid Get(HttpContext context) =>
        context.Items.TryGetValue(ItemKey, out var value) && value is Guid id ? id : CorrelationContext.Current ?? Guid.Empty;
}

public static class CorrelationHttpContextExtensions
{
    public static Guid GetCorrelationId(this HttpContext context) => CorrelationIdMiddleware.Get(context);
}
