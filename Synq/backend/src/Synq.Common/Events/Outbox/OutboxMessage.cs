using Microsoft.EntityFrameworkCore;
using Synq.Common.Events.Consuming;

namespace Synq.Common.Events.Outbox;

/// <summary>
/// Outbox lentelės eilutė (P-03). Įrašoma toje pačioje DB transakcijoje kaip būsenos pakeitimas,
/// todėl įvykis „egzistuoja“ tada ir tik tada, kai pakeitimas sėkmingai įrašytas (nėra dual-write problemos).
/// </summary>
public sealed class OutboxMessage
{
    /// <summary>= EventEnvelope.eventId ir AMQP message_id.</summary>
    public Guid Id { get; set; }
    public required string EventType { get; set; }
    public Guid AggregateId { get; set; }
    public Guid? WorkspaceId { get; set; }
    public Guid? CorrelationId { get; set; }
    public DateTime OccurredAtUtc { get; set; }

    /// <summary>Visas EventEnvelope JSON – relay jį publikuoja be pakeitimų.</summary>
    public required string Body { get; set; }

    /// <summary>null = dar nepublikuota. Nustatoma tik gavus brokerio patvirtinimą (publisher confirm).</summary>
    public DateTime? PublishedAtUtc { get; set; }
}

public static class OutboxModelBuilderExtensions
{
    /// <summary>
    /// Prideda <c>outbox</c> ir <c>processed_event</c> lenteles į serviso schemą.
    /// Kviesti iš kiekvieno serviso <c>DbContext.OnModelCreating</c>.
    /// </summary>
    public static ModelBuilder AddSynqOutbox(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxMessage>(e =>
        {
            e.ToTable("outbox");
            e.HasKey(x => x.Id);
            e.Property(x => x.EventType).HasMaxLength(100);
            e.Property(x => x.Body).HasColumnType("jsonb");
            // Dalinis indeksas: relay ieško tik nepublikuotų, o jų beveik visada mažai.
            e.HasIndex(x => x.OccurredAtUtc).HasFilter("published_at_utc IS NULL");
        });

        modelBuilder.Entity<ProcessedEvent>(e =>
        {
            e.ToTable("processed_event");
            e.HasKey(x => new { x.ConsumerName, x.EventId });
            e.Property(x => x.ConsumerName).HasMaxLength(100);
            e.HasIndex(x => x.ProcessedAtUtc);
        });

        return modelBuilder;
    }
}
