using System.Text.Json;
using Synq.Common.Json;

namespace Synq.Common.Events;

/// <summary>
/// Kiekvieno įvykio kūnas ant <c>synq.events</c> / <c>synq.transient</c> (yaml <c>EventEnvelope</c>, P-03).
/// AMQP savybės: message_id = EventId, correlation_id = užklausos correlation id, persistent.
/// <para>
/// <see cref="Payload"/> laikomas kaip <see cref="JsonElement"/>: vokas vienodas visiems tipams,
/// o vartotojas pats nusprendžia, į kurią <c>Event*</c> klasę jį deserializuoti (<see cref="PayloadAs{T}"/>).
/// </para>
/// </summary>
public sealed record EventEnvelope(
    Guid EventId,
    string EventType,
    Guid AggregateId,
    Guid? WorkspaceId,
    int SchemaVersion,
    DateTime OccurredAtUtc,
    JsonElement Payload)
{
    public T PayloadAs<T>() =>
        Payload.Deserialize<T>(SynqJson.Options) ?? throw new JsonException($"Empty payload for {EventType}.");
}
