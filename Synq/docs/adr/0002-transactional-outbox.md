# ADR 0002 – Transactional outbox

**Būsena:** priimta (P-03)

## Sprendimas
Būsenos pakeitimas ir `outbox` eilutė įrašomi vienoje DB transakcijoje. `OutboxRelay` (BackgroundService) kas 200 ms publikuoja į `synq.events` su publisher confirms ir nustato `publishedAtUtc`.
Vartotojai įrašo `(consumerName, eventId)` į `processed_event` toje pačioje transakcijoje (deduplikacija). Retry 1, 2, 4, 8, 16 s → `synq.dlq`.

## Pasekmės
- Išjungus RabbitMQ komandos vis tiek pavyksta, įvykiai publikuojami vėliau (at-least-once).
- Kiekvienas vartotojas turi būti idempotentiškas.
