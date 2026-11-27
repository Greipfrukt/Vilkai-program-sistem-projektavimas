using RabbitMQ.Client;

namespace Synq.Common.Events.Consuming;

/// <summary>
/// Gyvo fan-out eilė vienai instancijai (P-03; naudos Messaging R-05): <c>messaging.live.&lt;instanceId&gt;</c>.
/// <list type="bullet">
/// <item><b>exclusive</b> – tik ši jungtis gali ją naudoti;</item>
/// <item><b>auto-delete</b> – išnyksta, kai instancija atsijungia (restartas nieko nepraranda – klientai daro catch-up per REST);</item>
/// <item>kiekviena instancija turi <i>savo</i> eilę ⇒ kiekvienas įvykis pasiekia <i>visas</i> instancijas (ne konkuruoja).</item>
/// </list>
/// </summary>
/// <example>
/// var queue = await LiveQueue.DeclareAsync(channel, $"messaging.live.{instanceId}",
///     (RabbitMqTopology.EventsExchange, [EventTypes.MessageCreated, ...]),
///     (RabbitMqTopology.TransientExchange, [EventTypes.PresenceChanged]));
/// </example>
public static class LiveQueue
{
    public static async Task<string> DeclareAsync(
        IChannel channel, string queueName, params (string Exchange, IReadOnlyCollection<string> RoutingKeys)[] bindings)
    {
        await channel.QueueDeclareAsync(queueName, durable: false, exclusive: true, autoDelete: true);
        foreach (var (exchange, routingKeys) in bindings)
            foreach (var key in routingKeys)
                await channel.QueueBindAsync(queueName, exchange, key);
        return queueName;
    }
}
