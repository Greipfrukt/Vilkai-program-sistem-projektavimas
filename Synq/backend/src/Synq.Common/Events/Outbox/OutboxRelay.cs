using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Synq.Common.Time;

namespace Synq.Common.Events.Outbox;

/// <summary>
/// Kas 200 ms paima nepublikuotus outbox įrašus ir publikuoja juos į <c>synq.events</c>
/// (routing key = eventType) su publisher confirms; tik po patvirtinimo nustato <c>PublishedAtUtc</c> (P-03).
/// <para>
/// <b>At-least-once:</b> jei procesas nukrenta po publikavimo, bet prieš commit, įvykis bus publikuotas dar kartą –
/// todėl vartotojai deduplikuoja pagal eventId (<c>processed_event</c>).
/// </para>
/// <para>
/// <c>FOR UPDATE SKIP LOCKED</c> leidžia kelioms serviso instancijoms (messaging-1/2) dirbti lygiagrečiai
/// neperimant tų pačių eilučių.
/// </para>
/// </summary>
public sealed class OutboxRelay<TDb>(
    IServiceScopeFactory scopes,
    RabbitMqConnection rabbit,
    TimeProvider time,
    ILogger<OutboxRelay<TDb>> logger) : BackgroundService where TDb : DbContext
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(200);
    private const int BatchSize = 100;

    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var published = 0;
            try
            {
                published = await PublishBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Brokeris ar DB nepasiekiami – įvykiai lieka outbox'e ir bus publikuoti vėliau.
                logger.LogWarning("Outbox relay failed: {Error}", ex.Message);
                await DisposeChannelAsync();
                await Delay(TimeSpan.FromSeconds(2), stoppingToken);
                continue;
            }

            // Pilnas paketas → greičiausiai yra daugiau, imame iškart.
            if (published < BatchSize)
                await Delay(PollInterval, stoppingToken);
        }

        await DisposeChannelAsync();
    }

    private async Task<int> PublishBatchAsync(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TDb>();
        var schema = db.Model.GetDefaultSchema() ?? "public";

        // Schema – iš mūsų pačių EF modelio (ne vartotojo įvestis), todėl įterpti saugu.
        var sql = $"""
            SELECT * FROM "{schema}".outbox
            WHERE published_at_utc IS NULL
            ORDER BY occurred_at_utc
            LIMIT {BatchSize}
            FOR UPDATE SKIP LOCKED
            """;

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var batch = await db.Set<OutboxMessage>().FromSqlRaw(sql).ToListAsync(ct);

        if (batch.Count == 0)
            return 0;

        var channel = await GetChannelAsync(ct);
        var published = 0;
        try
        {
            foreach (var message in batch)
            {
                var properties = new BasicProperties
                {
                    MessageId = message.Id.ToString(),
                    CorrelationId = message.CorrelationId?.ToString(),
                    ContentType = "application/json",
                    Type = message.EventType,
                    DeliveryMode = DeliveryModes.Persistent,
                };
                // Su publisherConfirmationTrackingEnabled šis await baigiasi tik kai brokeris patvirtina (ack);
                // nack ar ryšio klaida → išimtis.
                await channel.BasicPublishAsync(
                    RabbitMqTopology.EventsExchange, message.EventType, mandatory: false,
                    properties, Encoding.UTF8.GetBytes(message.Body), ct);
                message.PublishedAtUtc = time.UtcNowMs();
                published++;
            }
        }
        finally
        {
            // Net jei dalis paketo nepavyko – išsaugome tai, kas jau patvirtinta.
            if (published > 0)
            {
                await db.SaveChangesAsync(CancellationToken.None);
                await tx.CommitAsync(CancellationToken.None);
            }
        }

        return published;
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken ct)
    {
        if (_channel is { IsOpen: true })
            return _channel;

        await DisposeChannelAsync();
        var connection = await rabbit.GetAsync(ct);
        _channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            ct);
        return _channel;
    }

    private async Task DisposeChannelAsync()
    {
        if (_channel is null)
            return;
        try { await _channel.DisposeAsync(); } catch { /* jau uždarytas */ }
        _channel = null;
    }

    private static async Task Delay(TimeSpan delay, CancellationToken ct)
    {
        try { await Task.Delay(delay, ct); } catch (OperationCanceledException) { }
    }
}
