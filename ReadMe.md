# Synq – V1

---

## 1. Scope (MoSCoW)

### 1.1 MUST – darom viską (nekeičiama)

FR-001–FR-015, FR-020–FR-021, FR-024–FR-035, visi NFR-001–NFR-015.

 **pirmiausia**:

| Rizika | Reikalavimai | Kodėl sunku |
|---|---|---|
| Bendras dokumentų redagavimas per 2 instancijas | FR-029–FR-035, NFR-011 | Yjs serveryje, idempotentiškumas, snapshot'ai, 100 000 simbolių riba |
| Reconnect / catch-up | FR-013, FR-033, NFR-006 | `seq` tarpai, laukiančių elementų eilė, state vector |
| Kelios Messaging instancijos | FR-012, FR-014, NFR-004, NFR-010 | Outbox + RabbitMQ + deduplikacija |
| Teisių atšaukimas per gyvą jungtį | FR-007, FR-028, NFR-008 | Reikia uždaryti WebSocket prenumeratas per ≤5 s |

### 1.2 SHOULD – maybe?

| ID | Funkcija | Sprendimas | Pagrindimas |
|---|---|---|---|
| FR-016 | Žinučių redagavimas ir trynimas | **Darom** (tik autoriaus) | Pigu: `edited` žymė + tombstone, įvykis jau yra architektūroje |
| FR-018 | Reakcijos | **Darom** | Viena lentelė su PK `(message_id, user_id, emoji_code)`, vienas įvykis |
| FR-038 | Dokumento pervadinimas ir trynimas | **Darom** | Soft delete jau suprojektuotas, reikia tik uždaryti sesijas |
| FR-016 (moderavimas) + FR-019 | Admin trina kitų žinutes + loginam | Tik po FR-016; audit lentelė maža |
| FR-017 | Thread'ai | **?** | Reikia atskiro UI panelio, reply skaičiavimų, papildomų WS įvykių |
| FR-022 | Paieška | **?** | Atskiras Search servisas + teisių filtravimas = daug darbo už mažą naudą |
| FR-023 | In-app pranešimai | **?** | Atskiras servisas, reikia apibrėžti mentions (dabar neapibrėžti tai xz) |

### 1.3 COULD – nedarom

FR-036 (rich text), FR-037 (ilgas offline redagavimas), FR-039 (versijų istorija) – **visi lieka už V1 ribų**.

### 1.4 Ką reikia pakeisti specifikacijoje dėl naujo scope

- **5.2 / 5.7:** Search ir Notification servisai neįtraukiami į Docker Compose.
- **8 skyrius:** W-10 (thread), W-12 (search), W-14 (notifications) pažymėti kaip neįgyvendinami V1.
- Atitinkamai pašalinti `message_mentions`, `notification`, `search` schemas iš 7.5 (arba 

### 1.5 Kur dar galima sumažinti krūvį (pasiūlymai, ne privaloma)

- **FR-003 profilio nuotrauka** – galima palikti tik display name, nuotrauką daryti paskutinę.


---

## 2. Tech stack

Abu variantai tinka. **Sprendimo kriterijus: kokią kalbą komanda moka geriau.**
Bet kuriuo atveju **frontas – React + TypeScript**

### TS

| Sluoksnis | Pasirinkimas |
|---|---|
| Repo | pnpm workspaces monorepo, `shared` paketas su Zod schemomis ir įvykių tipais |
| Frontend | React + Vite + TypeScript, TanStack Query, Zustand |
| Redaktorius | CodeMirror 6 + `y-codemirror.next` (remote caret'ai iš karto) |
| Backend | NestJS (Node 22 LTS) – Auth, Workspace, Messaging, Presence, File |
| Collaboration | Node + `ws` + `yjs` + `y-protocols` (savas serveris, ~300–500 eil.) |
| DB | PostgreSQL 17 + Drizzle ORM arba Kysely |
| Brokeris | RabbitMQ (`amqplib` arba `@golevelup/nestjs-rabbitmq`) |
| Presence | Redis (`ioredis`), TTL raktai |
| Failai | MinIO (S3), trumpalaikiai presigned URL |
| Gateway | Traefik (aktyvūs health check'ai, WebSocket) |
| Auth | argon2id; opaque session token HttpOnly cookie, DB saugomas SHA-256 hash |
| Kontraktai | OpenAPI (REST) + AsyncAPI (WebSocket ir įvykiai) |
| Logai | pino JSON, correlation ID, `/health` |
| Testai | Vitest, Testcontainers, Playwright (Chrome/Firefox/Edge), k6 (apkrova) |
| CI | GitHub Actions, Docker Compose |

### .NET

| Sluoksnis | Pasirinkimas |
|---|---|
| Runtime | .NET 10 (LTS), ASP.NET Core |
| Frontend | React + TypeScript (**ne** Blazor) |
| DB | EF Core + Npgsql (`HasFilter` daliniams unikaliems indeksams, `FromSql` su `FOR UPDATE`) |
| Brokeris | RabbitMQ.Client 7 + savas outbox relay (`BackgroundService`); alternatyva – Wolverine (MIT) |
| Chat real-time | SignalR kiekvienoje Messaging instancijoje **be** Redis backplane; RabbitMQ consumer → `IHubContext` |
| Dokumentų real-time | Raw ASP.NET Core WebSockets (Yjs provider'iai tikisi binarinio y-protocols) |
| Collaboration | **B1:** YDotNet (Yrs binding, v0.6.0, pre-1.0) **arba B2:** atskiras Node servisas su `yjs` |
| Presence | StackExchange.Redis |
| Failai | MinIO + AWSSDK.S3 |
| Gateway | YARP arba Traefik |
| Auth | ASP.NET Core `PasswordHasher` (PBKDF2) arba Argon2id; custom auth handler |
| Logai | Serilog JSON + OpenTelemetry, ASP.NET health checks |
| Testai | xUnit, Testcontainers for .NET, Playwright, k6 |


---

## 3. Klaidos, kurias reikia pataisyti

### 3.1 Reikalavimų specifikacija – loginės klaidos ir prieštaravimai

- [ ] **AuditRecord laužo nuosavybės taisyklę:** 7.3 diagrama deda AuditRecord į Workspace schemą, bet „Messaging writes MESSAGE_DELETED“. 7.5 lentelėje `audit_records` yra abiejose schemose. Pasirinkti: kiekvienas servisas turi savo lentelę **arba** Messaging siunčia įvykį.
- [ ] **FR-006 acceptance netinka:** FR-006 apie Admin rolės keitimą, o acceptance – „removed users lose access“. Pažemintas Admin lieka Member. Be to, **ownership transfer** neturi savo FR ir acceptance kriterijaus. (again we decide kaip handlinam)


### 3.2 Reikalavimų specifikacija (spragos)

- [ ] **Laukiantys kvietimai:** 3.1 sako „invitee sees pending invitations after signing in“ – nėra nei FR, nei wireframe.
- [ ] **FR-003:** ner aiškių formatų (yra tik 7.5: ≤2 MiB).
- [ ] **Priedų trynimas:** NFR-009 (MUST) reikalauja ištrinti priedus per 24 h, bet vienintelis trynimo kelias – FR-016 (SHOULD). Nėra ir taisyklės nepriskirtiems (orphan) įkėlimams.
- [ ] **Duomenų saugojimas:** 4.3 antraštėje minimas retention, bet aprašyti tik logai ir priedai (ne žinutės, dokumentai, pašalinto vartotojo duomenys).
- [ ] **2.1 „where applicable“:** aiškiai pasakyti, ar Owner/Admin gali **rašyti** privačiuose kanaluose, kuriuose nėra dalyviai. (we decide)

### 3.3 Techniniai nesutapimai (sutvarkyt)

- [ ] **100 000 simbolių riba:** specifikacija skaičiuoja Unicode code points, bet `Y.Text.length` skaičiuoja UTF-16 vienetus (emoji = 2). Arba perrašyti ribą UTF-16 vienetais, arba skaičiuoti `[...text].length` (TS) / `text.EnumerateRunes().Count()` (C#).
- [ ] **Yjs update atmetimas (UC-08 E2):** vėlesni to paties kliento update'ai priklauso nuo atmesto → serveris jų negali integruoti. Po atmetimo klientas turi išmesti Y.Doc, perkrauti būseną ir gauti naują clientID. Įrašyti į E2.
- [ ] **Reconnect backoff (3.5):** 1, 2, 4, 8 s → ketvirtas bandymas tik ~15 s, plius health check laikas → NFR-004 (15 s) neišpildomas. Riboti backoff iki 3–5 s su jitter.

---

## 4. Nuo ko pradėti

### 4.1 MAIN (prieš rašant funkcijas)

1. **Apsispręsti dėl stack'o** (TS ar .NET) – balsavimas, ne vėliau kaip 1 diena.
2. **Collaboration spike'as** (1 žmogus, 1 diena): 2 instancijos, 3 naršyklės, konvergencija, idempotentiškumas, simbolių riba. Tai didžiausia rizika – ją reikia nuimti pirmiausia.
3. **Repo struktūra:** monorepo, `docker-compose.yml` su PostgreSQL, RabbitMQ, Redis, MinIO, gateway ir tuščiais servisais su `/health`.
4. **CI:** GitHub Actions – build + lint + testai kiekvienam PR. Taisyklė: PR + 1 approval.
5. **Kontraktai:** užbaigti OpenAPI YAML ir parašyti AsyncAPI WebSocket kadrams ir įvykiams. Tada žmonės gali dirbti lygiagrečiai.
6. **Walking skeleton:** register → login → sukurti workspace → sukurti kanalą → išsiųsti žinutę, kurią mato kitas klientas kitoje instancijoje. Siauras, bet per visą sistemą.

### 4.2 Siūlomas darbų pasiskirstymas (4 žmonės)

| Atsakingas? | Sritis |
|---|---|
| - | Auth + Workspace (FR-001–FR-010), sesijos, kvietimai, rolės |
| - | Messaging (FR-011–FR-016, FR-018, FR-021), outbox, `seq`, deduplikacija |
| - | Collaboration (FR-027–FR-035, FR-038), Yjs, snapshot'ai |
| - | Frontend (visi ekranai, reconnect UI, pending/failed būsenos) |
| - | Presence + File (FR-020, FR-024–FR-026), gateway, Docker, CI, apkrovos testai (k6) |


### 4.3 Mėnesio planas

TBD

### 4.4 Bendri patarimai

- **Pirma MUST, tik paskui SHOULD.** Specifikacija (2.6) pati sako, kad SHOULD negali trukdyti MUST.
- **Testuoti per 2 instancijas nuo pirmos dienos**, ne paskutinę savaitę – tai pagrindinis kurso reikalavimas.
- **Acceptance kriterijai = testų sąrašas.** Kiekvienam MUST FR – bent vienas automatinis testas.
- **Dokumentaciją taisyti kartu su kodu:** pataisytos klaidos iš 3 skyriaus turi atsispindėti specifikacijoje prieš gynimą.