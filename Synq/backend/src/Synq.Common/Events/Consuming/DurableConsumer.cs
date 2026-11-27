using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Synq.Common.Http;
using Synq.Common.Json;
using Synq.Common.Time;

namespace Synq.Common.Events.Consuming;

/// <summary>
/// Patvarus įvykių vartotojas (P-03). Kiekvienas servisas paveldi ir nurodo eilę, įvykių tipus ir apdorojimą.
/// <list type="bullet">
/// <item>Eilė durable, surišta su <c>synq.events</c>; tos pačios eilės vartotojai (instancijos) konkuruoja.</item>
/// <item>Įvykio poveikis ir <c>processed_event</c> įrašas – vienoje transakcijoje ⇒ dublis apdorojamas vieną kartą.</item>
/// <item>Klaida → pakartojama po 1, 2, 4, 8, 16 s; po 5 nesėkmių → <c>synq.dlq</c> (su klaida antraštėse) ir ack.</item>
/// <item>prefetch = 1 – įvykiai apdorojami po vieną, išlaikant brokerio tvarką.</item>
/// </list>
/// </summary>
/// <example>
/// public sealed class ProfileConsumer(...) : DurableConsumer&lt;WorkspaceDbContext&gt;(...)
/// {
///     protected override string QueueName =&gt; "workspace.events";
///     protected override IReadOnlyCollection&lt;string&gt; EventTypes =&gt; [Events.EventTypes.UserProfileUpdated];
///     protected override Task HandleAsync(EventEnvelope e, WorkspaceDbContext db, IServiceProvider sp, CancellationToken ct) { ... }
/// }
/// </example>
public abstract class DurableConsumer<TDb>(
    IServiceScopeFactory scopes,
    RabbitMqConnection rabbit,
    TimeProvider time,
    ILogger logger) : BackgroundService where TDb : DbContext
{
    private static readonly TimeSpan[] DefaultRetryDelays =
        [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(16)];

    /// <summary>Eilės pavadinimas, pvz. <c>workspace.events</c>. Kartu tai ir consumerName processed_event lentelėje.</summary>
    protected abstract string QueueName { get; }

    /// <summary>Kurių tipų įvykius gauti (routing keys).</summary>
    protected abstract IReadOnlyCollection<string> EventTypes { get; }

    /// <summary>Įvykio poveikis. Nekviesti SaveChanges – tai padaro bazinė klasė kartu su processed_event.</summary>
    protected abstract Task HandleAsync(EventEnvelope envelope, TDb db, IServiceProvider services, CancellationToken ct);

    /// <summary>Testams galima sutrumpinti.</summary>
    protected virtual IReadOnlyList<TimeSpan> RetryDelays => DefaultRetryDelays;

    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SubscribeAsync(stoppingToken);
                // Toliau jungtį ir prenumeratą prižiūri RabbitMQ klientas (automatic recovery).
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning("Consumer {Queue} cannot subscribe yet: {Error}", QueueName, ex.Message);
                try { await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken); } catch (OperationCanceledException) { break; }
            }
        }

        if (_channel is not null)
            await _channel.DisposeAsync();
    }

    private async Task SubscribeAsync(CancellationToken ct)
    {
        var connection = await rabbit.GetAsync(ct);
        var channel = await connection.CreateChannelAsync(cancellationToken: ct);
        await channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        foreach (var eventType in EventTypes)
            await channel.QueueBindAsync(QueueName, RabbitMqTopology.EventsExchange, eventType, cancellationToken: ct);
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: ct);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, delivery) => OnDeliveryAsync(channel, delivery, ct);
        await channel.BasicConsumeAsync(QueueName, autoAck: false, consumer, ct);
        _channel = channel;
        logger.LogInformation("Consumer {Queue} subscribed to {EventTypes}", QueueName, EventTypes);
    }

    private async Task OnDeliveryAsync(IChannel channel, BasicDeliverEventArgs delivery, CancellationToken ct)
    {
        var body = delivery.Body.ToArray();
        var correlationId = Guid.TryParse(delivery.BasicProperties.CorrelationId, out var c) ? c : Guid.NewGuid();
        using var _ = CorrelationContext.Begin(correlationId);

        EventEnvelope? envelope = null;
        try
        {
            envelope = JsonSerializer.Deserialize<EventEnvelope>(body, SynqJson.Options);
        }
        catch (JsonException)
        {
        }

        if (envelope is null)
        {
            await DeadLetterAsync(channel, body, delivery, "Malformed event envelope", ct);
            return;
        }

        Exception? lastError = null;
        for (var attempt = 0; attempt <= RetryDelays.Count; attempt++)
        {
            if (attempt > 0)
                await Task.Delay(RetryDelays[attempt - 1], ct);
            try
            {
                await ProcessOnceAsync(envelope, ct);
                await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, ct);
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lastError = ex;
                logger.LogWarning(ex, "Consumer {Queue} failed on {EventType} {EventId} (attempt {Attempt})",
                    QueueName, envelope.EventType, envelope.EventId, attempt + 1);
            }
        }

        await DeadLetterAsync(channel, body, delivery, lastError?.Message ?? "Unknown error", ct);
    }

    private async Task ProcessOnceAsync(EventEnvelope envelope, CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TDb>();
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var alreadyProcessed = await db.Set<ProcessedEvent>()
            .AnyAsync(p => p.ConsumerName == QueueName && p.EventId == envelope.EventId, ct);
        if (alreadyProcessed)
        {
            logger.LogInformation("Consumer {Queue} skipped duplicate {EventId}", QueueName, envelope.EventId);
            return;
        }

        await HandleAsync(envelope, db, scope.ServiceProvider, ct);
        db.Set<ProcessedEvent>().Add(new ProcessedEvent
        {
            ConsumerName = QueueName,
            EventId = envelope.EventId,
            ProcessedAtUtc = time.UtcNowMs(),
        });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    private async Task DeadLetterAsync(IChannel channel, byte[] body, BasicDeliverEventArgs delivery, string error, CancellationToken ct)
    {
        logger.LogError("Consumer {Queue} moved event {MessageId} to {Dlq}: {Error}",
            QueueName, delivery.BasicProperties.MessageId, RabbitMqTopology.DeadLetterQueue, error);

        var properties = new BasicProperties
        {
            MessageId = delivery.BasicProperties.MessageId,
            CorrelationId = delivery.BasicProperties.CorrelationId,
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            Headers = new Dictionary<string, object?>
            {
                ["x-consumer"] = QueueName,
                ["x-routing-key"] = delivery.RoutingKey,
                ["x-error"] = Encoding.UTF8.GetBytes(error.Length > 500 ? error[..500] : error),
            },
        };
        await channel.BasicPublishAsync("", RabbitMqTopology.DeadLetterQueue, mandatory: false, properties, body, ct);
        await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, ct);
    }
}
