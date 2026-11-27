namespace Synq.Common.Events.Consuming;

/// <summary>
/// processed_event (consumerName, eventId) – deduplikacija (P-03). Įrašoma toje pačioje transakcijoje
/// kaip įvykio poveikis: jei transakcija nepavyksta, nėra ir įrašo, todėl įvykis bus apdorotas iš naujo.
/// </summary>
public sealed class ProcessedEvent
{
    public required string ConsumerName { get; set; }
    public Guid EventId { get; set; }
    public DateTime ProcessedAtUtc { get; set; }
}
