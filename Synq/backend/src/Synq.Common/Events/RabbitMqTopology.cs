using RabbitMQ.Client;

namespace Synq.Common.Events;

/// <summary>
/// Bendri brokerio elementai (P-03, yaml „Broker and events“). Deklaravimas idempotentiškas –
/// kiekvienas servisas jį kartoja prisijungdamas, todėl paleidimo tvarka nesvarbi.
/// </summary>
public static class RabbitMqTopology
{
    /// <summary>Visi domeno įvykiai per outbox. topic, durable, routing key = eventType.</summary>
    public const string EventsExchange = "synq.events";

    /// <summary>Trumpalaikiai įvykiai (presence.changed). topic, non-durable.</summary>
    public const string TransientExchange = "synq.transient";

    /// <summary>„Nuodingi“ įvykiai po 5 nepavykusių bandymų. Durable eilė (per default exchange).</summary>
    public const string DeadLetterQueue = "synq.dlq";

    public static async Task DeclareAsync(IConnection connection, CancellationToken ct)
    {
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
        await channel.ExchangeDeclareAsync(EventsExchange, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: ct);
        await channel.ExchangeDeclareAsync(TransientExchange, ExchangeType.Topic, durable: false, autoDelete: false, cancellationToken: ct);
        await channel.QueueDeclareAsync(DeadLetterQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
    }
}
