# ADR 0003 – Žinučių tvarka pagal `seq`

**Būsena:** priimta (R-02)

## Sprendimas
Kiekvienas pokalbis turi `last_seq`; siuntimas: `SELECT ... FOR UPDATE` → `last_seq + 1` → insert + outbox vienoje transakcijoje. Idempotencija – unikalus `(conversationId, clientRequestId)`.

Klientai (web `seqTracker.ts`, iOS `SeqTracker.swift`) taiko tą patį algoritmą:
- `seq == last + 1` → rodyti;
- `seq > last + 1` → į buferį ir catch-up per REST istoriją;
- jau žinomas `seq` → ignoruoti.

## Pasekmės
Tvarka nepriklauso nuo to, per kurią Messaging instanciją atėjo įvykis; reconnect = catch-up nuo paskutinio nuoseklaus `seq`.
