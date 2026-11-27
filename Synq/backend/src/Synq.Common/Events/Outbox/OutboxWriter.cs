using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Synq.Common.Http;
using Synq.Common.Json;
using Synq.Common.Time;

namespace Synq.Common.Events.Outbox;

/// <summary>
/// Įvykio įrašymas į outbox (P-03). Tik prideda eilutę į DbContext – įrašoma kartu su kitais
/// pakeitimais tuo pačiu <c>SaveChangesAsync</c> (viena transakcija).
/// </summary>
/// <example>
/// db.Workspaces.Add(workspace);
/// outbox.Enqueue(db, EventTypes.WorkspaceMemberAdded, workspace.Id, workspace.Id, new MembershipChanged(...));
/// await db.SaveChangesAsync(ct);   // ← ir workspace, ir įvykis, arba nieko
/// </example>
public sealed class OutboxWriter(TimeProvider time)
{
    public const int SchemaVersion = 1;

    public Guid Enqueue<TPayload>(DbContext db, string eventType, Guid aggregateId, Guid? workspaceId, TPayload payload)
    {
        var eventId = Guid.CreateVersion7();
        var occurredAt = time.UtcNowMs();
        var envelope = new EventEnvelope(
            eventId, eventType, aggregateId, workspaceId, SchemaVersion, occurredAt,
            JsonSerializer.SerializeToElement(payload, SynqJson.Options));

        db.Set<OutboxMessage>().Add(new OutboxMessage
        {
            Id = eventId,
            EventType = eventType,
            AggregateId = aggregateId,
            WorkspaceId = workspaceId,
            CorrelationId = CorrelationContext.Current,
            OccurredAtUtc = occurredAt,
            Body = JsonSerializer.Serialize(envelope, SynqJson.Options),
        });
        return eventId;
    }
}
