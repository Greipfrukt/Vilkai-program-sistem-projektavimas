# Synq – repozitorijos medis ir nuo ko pradėti

Pagal *Synq V1 – darbų planas ir paskirstymas* (2026-11-23 – 12-13).
Komanda: **Pijus** (infra, Auth, Workspace), **Rokas** (Messaging, Presence), **Artūras** (web, Search, Notification), **Augustinas** (iOS).

Prie aplankų ir failų nurodyti užduočių ID iš plano (P-xx, R-xx, A-xx, I-xx), kad būtų aišku, kas ką kuria.

---

## 1. Repozitorijos medis

```
synq/
├── README.md                              # P-01, P-10 – paleidimas per 10 min, architektūra, demo scenarijus
├── Synq.yaml                              # KONTRAKTAS – keičiamas tik tame pačiame PR kaip kodas
├── docker-compose.yml                     # P-01 – visa sistema
├── docker-compose.override.yml            # P-01 – dev nustatymai (Cookie:Secure=false, portai)
├── .env.example                           # P-01 – visi kintamieji su saugiomis reikšmėmis
├── .gitignore                             # P-01 – bin/, obj/, node_modules/, DerivedData/, .env
├── .editorconfig
│
├── .github/
│   ├── workflows/
│   │   ├── backend.yml                    # dotnet build + test (Testcontainers)
│   │   ├── web.yml                        # npm ci, lint, build
│   │   └── contract.yml                   # Synq.yaml validacija (spectral / redocly lint)
│   └── pull_request_template.md           # užduoties ID, FR, testas, yaml pakeistas?
│
├── backend/
│   ├── Synq.sln                           # P-01
│   ├── Directory.Build.props              # P-01 – net8.0, nullable, warnings as errors
│   ├── Directory.Packages.props           # P-01 – centralizuotos paketų versijos
│   ├── global.json                        # P-01 – .NET 8 SDK (8.0.x), rollForward: latestFeature
│   │
│   ├── src/
│   │   ├── Synq.Common/                   # P-02, P-03 – NAUDOJA VISI SERVISAI
│   │   │   ├── Errors/
│   │   │   │   ├── ErrorCodes.cs          # visas klaidų kodų katalogas iš yaml
│   │   │   │   ├── ApiError.cs            # code, message, correlationId, fieldErrors
│   │   │   │   └── ExceptionMiddleware.cs # → 500 INTERNAL_ERROR
│   │   │   ├── Http/
│   │   │   │   ├── CorrelationIdMiddleware.cs
│   │   │   │   ├── GatewayUserContext.cs  # X-User-Id, X-Session-Id
│   │   │   │   ├── InternalTokenFilter.cs # X-Internal-Token /internal/**
│   │   │   │   └── Paging.cs              # offset, limit ≤ 100, sort parseris (→ 400)
│   │   │   ├── Logging/
│   │   │   │   └── SerilogSetup.cs        # JSON, be slaptažodžių/tokenų/žinučių teksto
│   │   │   ├── Health/
│   │   │   │   └── HealthSetup.cs         # /health/live, /health/ready
│   │   │   └── Events/
│   │   │       ├── EventEnvelope.cs
│   │   │       ├── EventTypes.cs          # "message.created", "workspace.member-added"...
│   │   │       ├── Payloads/              # visi Event* payload'ai (be failų/dokumentų)
│   │   │       ├── Outbox/
│   │   │       │   ├── OutboxMessage.cs
│   │   │       │   ├── OutboxWriter.cs    # įrašo toje pačioje transakcijoje
│   │   │       │   └── OutboxRelay.cs     # BackgroundService, kas 200 ms, publisher confirms
│   │   │       ├── Consuming/
│   │   │       │   ├── ProcessedEvent.cs  # (consumerName, eventId)
│   │   │       │   ├── DurableConsumer.cs # retry 1,2,4,8,16 s → synq.dlq
│   │   │       │   └── LiveQueue.cs       # exclusive, auto-delete eilės Messaging'ui
│   │   │       └── RabbitMqTopology.cs    # synq.events, synq.transient, synq.dlq
│   │   │
│   │   ├── Synq.Gateway/                  # P-04 – YARP
│   │   │   ├── Program.cs
│   │   │   ├── appsettings.json           # maršrutai pagal yaml „Gateway routing“
│   │   │   ├── SessionValidationMiddleware.cs   # /internal/v1/sessions/validate + Redis cache
│   │   │   ├── OriginCheck.cs             # WS upgrade; leisti iOS Origin
│   │   │   └── Limits.cs                  # 3 sav.: 429, 413, 504, 503
│   │   │
│   │   ├── Synq.Auth/                     # P-05 – FR-001–003
│   │   │   ├── Program.cs
│   │   │   ├── Api/
│   │   │   │   ├── UsersEndpoints.cs      # POST /users, GET/PATCH /users/me, GET /users/{id}
│   │   │   │   ├── SessionsEndpoints.cs   # POST /sessions, GET/DELETE /sessions/current
│   │   │   │   └── InternalEndpoints.cs   # POST /internal/v1/sessions/validate
│   │   │   ├── Domain/
│   │   │   │   ├── PasswordPolicy.cs
│   │   │   │   ├── SessionToken.cs        # 32 baitai base64url, SHA-256
│   │   │   │   └── LoginRateLimiter.cs    # 10 nesėkmių / 15 min
│   │   │   ├── Data/
│   │   │   │   ├── AuthDbContext.cs       # schema "auth"
│   │   │   │   ├── Entities/              # User, Session, WorkspaceMembershipProjection
│   │   │   │   └── Migrations/
│   │   │   ├── Consumers/                 # workspace.member-* → narysčių projekcija
│   │   │   └── Dockerfile
│   │   │
│   │   ├── Synq.Workspace/                # P-06, P-07, P-08, P-09 – FR-004–010
│   │   │   ├── Program.cs
│   │   │   ├── Api/
│   │   │   │   ├── WorkspacesEndpoints.cs
│   │   │   │   ├── MembersEndpoints.cs    # rolės, šalinimas, ownership-transfers
│   │   │   │   ├── InvitationsEndpoints.cs
│   │   │   │   ├── ChannelsEndpoints.cs
│   │   │   │   ├── ParticipantsEndpoints.cs
│   │   │   │   └── InternalAccessEndpoints.cs   # GET /internal/v1/access, channel-ids
│   │   │   ├── Domain/
│   │   │   │   ├── AccessRules.cs         # VISOS teisių taisyklės vienoje vietoje (fail closed)
│   │   │   │   ├── Roles.cs
│   │   │   │   └── InvitationToken.cs
│   │   │   ├── Data/
│   │   │   │   ├── WorkspaceDbContext.cs  # schema "workspace"
│   │   │   │   ├── Entities/
│   │   │   │   └── Migrations/
│   │   │   ├── Consumers/                 # user.profile-updated → UserProfileProjection
│   │   │   └── Dockerfile
│   │   │
│   │   ├── Synq.Messaging/                # R-01 – R-08 – FR-011–019, 021 (2 instancijos)
│   │   │   ├── Program.cs
│   │   │   ├── Api/
│   │   │   │   ├── MessagesEndpoints.cs   # siuntimas, istorija, PATCH/DELETE, replies
│   │   │   │   ├── ConversationsEndpoints.cs
│   │   │   │   ├── ReactionsEndpoints.cs
│   │   │   │   └── ReadCursorEndpoints.cs
│   │   │   ├── Realtime/
│   │   │   │   ├── WsEndpoint.cs          # /ws upgrade, kadrų ciklas
│   │   │   │   ├── WsConnection.cs        # vienas prisijungimas: user, session, workspace
│   │   │   │   ├── ConnectionRegistry.cs  # kas prisijungęs prie ŠIOS instancijos
│   │   │   │   ├── Frames.cs              # session.ready, workspace.select, heartbeat...
│   │   │   │   ├── CloseCodes.cs          # 4001, 4003, 4008, 4029, 1001
│   │   │   │   ├── LiveEventFanout.cs     # messaging.live.<instanceId> → push klientams
│   │   │   │   └── RevocationHandler.cs   # member-removed → access.revoked per ≤ 5 s
│   │   │   ├── Domain/
│   │   │   │   ├── SendMessage.cs         # FOR UPDATE → last_seq + 1 → insert + outbox
│   │   │   │   ├── Mentions.cs            # <@userId> parseris
│   │   │   │   ├── EditDelete.cs          # editVersion, tombstone
│   │   │   │   ├── Threads.cs
│   │   │   │   ├── Reactions.cs
│   │   │   │   ├── Emoji.cs               # palaikomų shortcode'ų sąrašas
│   │   │   │   └── Unread.cs
│   │   │   ├── Clients/
│   │   │   │   ├── WorkspaceAccessClient.cs   # GET /internal/v1/access
│   │   │   │   └── PresenceClient.cs          # heartbeat PUT/DELETE
│   │   │   ├── Data/
│   │   │   │   ├── MessagingDbContext.cs  # schema "messaging"
│   │   │   │   ├── Entities/
│   │   │   │   └── Migrations/
│   │   │   ├── Consumers/                 # channel.*, workspace.member-*, user.profile-updated
│   │   │   └── Dockerfile
│   │   │
│   │   ├── Synq.Presence/                 # R-09 – FR-020
│   │   │   ├── Program.cs
│   │   │   ├── Api/PresenceEndpoints.cs   # internal heartbeat + GET /workspaces/{id}/presence
│   │   │   ├── PresenceStore.cs           # Redis, TTL 15 s
│   │   │   ├── ExpiryWatcher.cs           # kas 5 s → presence.changed (synq.transient)
│   │   │   ├── Consumers/
│   │   │   └── Dockerfile
│   │   │
│   │   ├── Synq.Search/                   # A-07 – FR-022 (SHOULD)
│   │   │   ├── Program.cs
│   │   │   ├── Api/SearchEndpoints.cs
│   │   │   ├── Indexer.cs                 # message.created/updated/deleted → tsvector
│   │   │   ├── AccessFilter.cs            # channel-ids + conversation-ids, nepavyko → 503
│   │   │   ├── Data/                      # schema "search", GIN indeksas
│   │   │   └── Dockerfile
│   │   │
│   │   └── Synq.Notification/             # A-08 – FR-023 (SHOULD)
│   │       ├── Program.cs
│   │       ├── Api/NotificationsEndpoints.cs
│   │       ├── NotificationCreator.cs     # DIRECT_MESSAGE, MENTION; unikalus (recipient, sourceEvent)
│   │       ├── Data/                      # schema "notification"
│   │       └── Dockerfile
│   │
│   └── tests/
│       ├── Synq.TestKit/                  # P-02 – bendri Testcontainers fixture'ai (Postgres, RabbitMQ, Redis)
│       ├── Synq.Common.Tests/             # P-03 – outbox be RabbitMQ, dvigubas įvykis
│       ├── Synq.Auth.Tests/               # P-05
│       ├── Synq.Workspace.Tests/          # P-06–P-10 – teisių matrica
│       ├── Synq.Messaging.Tests/          # R-02 – 50 lygiagrečių žinučių → seq be tarpų
│       ├── Synq.Presence.Tests/
│       ├── Synq.Search.Tests/
│       └── Synq.Notification.Tests/
│
├── web/                                   # Artūras – A-01 – A-09
│   ├── package.json
│   ├── vite.config.ts                     # proxy /api → gateway arba Prism (:4010)
│   ├── tsconfig.json
│   ├── .env.development                   # VITE_API_TARGET=mock | backend
│   ├── index.html
│   ├── scripts/
│   │   └── gen-api.sh                     # tipai iš ../Synq.yaml (openapi-typescript)
│   └── src/
│       ├── main.tsx
│       ├── App.tsx                        # maršrutai
│       ├── api/
│       │   ├── schema.d.ts                # SUGENERUOTA – nekeisti ranka
│       │   ├── client.ts                  # fetch, credentials:'include', 401, 429, Error
│       │   └── endpoints/                 # auth.ts, workspaces.ts, messages.ts...
│       ├── realtime/
│       │   ├── socket.ts                  # A-04 – heartbeat 10 s, reconnect 0,5…8 s ±20 %
│       │   ├── closeCodes.ts
│       │   ├── seqTracker.ts              # paskutinis nuoseklus seq, buferis, catch-up
│       │   └── outbox.ts                  # siuntimo eilė su clientRequestId
│       ├── features/
│       │   ├── auth/                      # A-02 – W-01, W-02, profilis
│       │   ├── workspaces/                # A-03 – juosta, nariai, kvietimai, /invite/:token
│       │   ├── channels/                  # A-03 – sąrašas, katalogas, kūrimas, archyvas
│       │   ├── conversation/              # A-05 – istorija, composer, DM/grupė, mentions
│       │   ├── presence/
│       │   ├── threads/                   # A-06 (SHOULD)
│       │   ├── reactions/                 # A-06 (SHOULD)
│       │   ├── search/                    # A-07 (SHOULD)
│       │   └── notifications/             # A-08 (SHOULD)
│       ├── components/                    # A-09 – Loading, AccessDenied, ConnectionBanner...
│       └── state/                         # sesija, pasirinktas workspace, neperskaitytos
│
├── ios/                                   # Augustinas – I-01 – I-09 (tik Mac)
│   ├── Synq.xcodeproj
│   ├── Synq/                              # SwiftUI programėlė
│   │   ├── SynqApp.swift                  # scenePhase → WS uždarymas/prisijungimas
│   │   ├── Info.plist                     # NSAllowsLocalNetworking = YES
│   │   ├── Config.swift                   # baseURL, Origin reikšmė
│   │   └── Features/
│   │       ├── Auth/                      # I-03 – LoginView, RegisterView, ProfileView
│   │       ├── Workspaces/                # I-04
│   │       ├── Channels/                  # I-04
│   │       ├── Conversation/              # I-06 – MessageListView, ComposerView
│   │       ├── Threads/                   # I-07 (SHOULD)
│   │       ├── Search/                    # I-08 (SHOULD)
│   │       ├── Notifications/             # I-08 (SHOULD)
│   │       └── Shared/                    # I-09 – ConnectionBanner, tušti/krovimo ekranai
│   └── SynqKit/                           # I-02 – Swift Package, peržiūrimas be Mac
│       ├── Package.swift
│       ├── Sources/SynqKit/
│       │   ├── Models/                    # Codable pagal Synq.yaml
│       │   ├── API/
│       │   │   ├── APIClient.swift        # async/await, ISO-8601 su ms
│       │   │   └── APIError.swift         # su correlationId
│       │   ├── Session/SessionStore.swift
│       │   └── Realtime/
│       │       ├── WebSocketManager.swift # actor, heartbeat, reconnect
│       │       ├── SeqTracker.swift       # TIE PATYS principai kaip web/realtime/seqTracker.ts
│       │       └── SendQueue.swift
│       └── Tests/SynqKitTests/            # swift test
│
├── tests/                                 # visa sistema per docker compose
│   ├── seed/
│   │   ├── seed.sql                       # laikini duomenys, kol nėra P-06/P-08
│   │   └── seed-load.js                   # A-09 – 100 vartotojų, 50 workspace'ų
│   ├── load/                              # A-09 – k6
│   │   ├── ws-100-clients.js              # NFR-001
│   │   ├── api-latency.js                 # NFR-002
│   │   ├── cross-instance-delivery.js     # NFR-003
│   │   └── REPORT.md                      # p95 rezultatai
│   ├── failure/                           # R-10
│   │   ├── stop-messaging-1.sh            # NFR-004
│   │   ├── restart-messaging.sh           # NFR-005
│   │   ├── stop-rabbitmq.sh               # outbox
│   │   ├── lost-ack-retry.sh              # idempotencija
│   │   └── README.md                      # kiekvieno scenarijaus aprašas ir laukiamas rezultatas
│   └── demo/
│       └── E2E-scenarijus.md              # I-09 – pristatymo žingsniai
│
├── infra/
│   ├── postgres/init.sql                  # P-01 – schemos: auth, workspace, messaging, search, notification
│   └── rabbitmq/definitions.json          # P-03 – synq.events, synq.transient, synq.dlq
│
└── docs/
    ├── Synq_specifikacija.docx
    ├── Synq_darbu_planas.pdf
    └── adr/
        ├── 0001-dotnet-yarp.md
        ├── 0002-transactional-outbox.md
        └── 0003-seq-ordering.md
```

### docker compose servisai

```
postgres        :5432
rabbitmq        :5672, UI :15672
redis           :6379
gateway         :8080   ← vienintelis portas, kurį naudoja web ir iOS
auth
workspace
messaging-1  ┐  tas pats image, 2 instancijos
messaging-2  ┘
presence
search
notification
(web – per `npm run dev`, Vite proxy į :8080)
(Prism mock – :4010, tik 1 savaitę)
```

---

## 2. Nuo kurių failų pradėti

Svarbiausia: **pirmadienio vakarą (11-23) main šakoje turi būti tuščias, bet besikompiliuojantis skeletas**, kad visi galėtų pradėti savo šakas nuo jo. Kritinis kelias: P-01 → P-02 → P-03 iki **trečiadienio 11-25**.

### 2.1 Pirmoji diena – visi kartu (Pijus pradeda, kiti peržiūri)

| Eilė | Failas | Kas | Kodėl pirmas |
|---|---|---|---|
| 1 | `Synq.yaml` (įkelti esamą) | Pijus | Visų darbų pagrindas, iš jo eina Prism mock ir tipai |
| 2 | `.gitignore`, `.editorconfig`, `README.md` (paleidimo skeletas) | Pijus | Kad niekas nepush'intų `bin/`, `node_modules/`, `DerivedData/` |
| 3 | `backend/Synq.sln`, `Directory.Build.props`, `Directory.Packages.props`, `global.json` | Pijus | Visi projektai turi vienodus nustatymus ir paketų versijas |
| 4 | Tušti `Synq.*/Program.cs` + `Dockerfile` kiekvienam servisui | Pijus | Rokas ir Artūras gali iškart kurti savo šakas |
| 5 | `infra/postgres/init.sql` | Pijus | Schemos visiems servisams |
| 6 | `docker-compose.yml` | Pijus | `docker compose up` turi pasileisti (net su tuščiais servisais) |
| 7 | `.github/pull_request_template.md`, `backend.yml` | Pijus | Nuo pirmo PR veikia CI |

### 2.2 Kiekvieno pirmieji failai (1 savaitė)

**Pijus (kritinis kelias)**
1. `Synq.Common/Health/HealthSetup.cs` – kad compose rodytų `healthy`.
2. `Synq.Common/Errors/*` + `Http/CorrelationIdMiddleware.cs` – visi servisai grąžina tą patį klaidų formatą.
3. `Synq.Common/Events/Outbox/OutboxWriter.cs` → `OutboxRelay.cs` → `Consuming/DurableConsumer.cs` + `Synq.Common.Tests` (išjungtas RabbitMQ, dvigubas įvykis). **Iki 11-25.**
4. `Synq.Auth/Data/AuthDbContext.cs` → `Domain/SessionToken.cs` → `Api/SessionsEndpoints.cs` → `Api/UsersEndpoints.cs`.
5. `Synq.Gateway/appsettings.json` + `SessionValidationMiddleware.cs`. **Iki 11-27.**
6. `Synq.Workspace/Api/InternalAccessEndpoints.cs` – minimali versija (tik narystė), kad Rokas neužstrigtų. **Iki 11-27.**

**Rokas**
1. `Synq.Messaging/Data/Entities/*` + `MessagingDbContext.cs` + pirmoji migracija (R-01). Peržiūrėti su Pijumi.
2. `Domain/SendMessage.cs` + testas `Synq.Messaging.Tests/SendMessageConcurrencyTests.cs` (50 lygiagrečių žinučių). Kol nėra P-03 – naudoti `IOutboxWriter` interfeisą su laikina implementacija.
3. `Api/MessagesEndpoints.cs` – siuntimas ir istorija (R-02, R-03).
4. `Realtime/WsEndpoint.cs` → `Frames.cs` → `WsConnection.cs` – minimalus WS vienai instancijai (`session.ready`, `workspace.select`, `heartbeat`, `message.created`). **Iki 11-27** – nuo to priklauso Augustinas ir Artūras.

**Artūras**
1. `web/package.json`, `vite.config.ts`, `.env.development` – Vite + React, proxy į Prism (`:4010`).
2. `web/scripts/gen-api.sh` → `src/api/schema.d.ts` (sugeneruota iš `Synq.yaml`).
3. `src/api/client.ts` – vienas fetch wrapper'is su klaidų apdorojimu (A-01).
4. `src/features/auth/*` – prisijungimas ir registracija prieš mock (A-02).
5. Kartu su Augustinu: **`src/realtime/seqTracker.ts`** – aprašyti algoritmą (seq = last+1 → rodyti; didesnis → buferis + catch-up; žinomas → ignoruoti) ir tą patį aprašymą perkelti į `SeqTracker.swift`.

**Augustinas**
1. `ios/Synq.xcodeproj` + `Info.plist` (`NSAllowsLocalNetworking`) + `Config.swift` (baseURL, Origin).
2. `SynqKit/Package.swift` → `Models/` (User, Workspace, Channel, Message) → `API/APIClient.swift`.
3. `Features/Auth/LoginView.swift` – `POST /sessions` prieš Prism mock.
4. `Realtime/WebSocketManager.swift` – prisijungimas su rankiniu `Origin`, `message.created` parodymas.
5. **Iki 11-29:** simuliatoriuje matosi realiu laiku iš web'o atsiųsta žinutė (I-01). Jei cookie, Origin arba WS neveikia – iškart rašyti Pijui ir Rokui.

### 2.3 Taisyklės, kurias verta susitarti pirmą dieną

- **`Synq.yaml` keičiamas tik PR'e kartu su kodu**, ir abu klientai po to iš naujo sugeneruoja tipus / modelius.
- **`AccessRules.cs` – vienintelė vieta, kur rašomos teisių taisyklės.** Kiti servisai jų nekartoja, o klausia `/internal/v1/access`.
- **Šakos:** `feature/P-01-repo-setup`, `feature/R-02-send-message`, `feature/A-04-websocket`, `feature/I-01-prototype`.
- **Ketvirtadienio integracija** (11-26): visi paleidžia `main` per `docker compose up` ir praeina M1 scenarijų.

---

## 3. Sprendimas: backend – .NET 8

Nuspręsta: **backend'as rašomas .NET 8** (ne .NET 10). Darbų plane 1 skyriaus lentelėje pakeisti „.NET 10“ į „.NET 8“.

- `Directory.Build.props`: `<TargetFramework>net8.0</TargetFramework>`.
- `global.json`: .NET 8 SDK, kad visi (ir Augustino Mac) naudotų tą pačią versiją.
- Paketus pasirinkti **8.x** versijas: EF Core 8, Npgsql.EntityFrameworkCore.PostgreSQL 8, Microsoft.AspNetCore.* 8. YARP, Serilog, RabbitMQ.Client, StackExchange.Redis ir Testcontainers palaiko .NET 8.
- Docker image'ai: `mcr.microsoft.com/dotnet/sdk:8.0` (build) ir `mcr.microsoft.com/dotnet/aspnet:8.0` (runtime). Abu turi arm64 versijas Mac'ui.
- **Svarbu:** .NET 8 palaikymas baigėsi **2026-11-10**, todėl saugumo atnaujinimų nebebus. Universitetiniam projektui, kuris veikia tik lokaliai per Docker Compose, tai nėra problema. Vis dėlto gynime verta pasakyti, kad versija pasirinkta sąmoningai (pvz. todėl, kad komanda ją moka), o tikram produktui reikėtų .NET 10 LTS.

## 4. Sprendimas: web – TypeScript

Nuspręsta: **web rašomas TypeScript**. Darbų plane reikia pataisyti:

- **A-01:** „Vite + React + JavaScript“ → „Vite + React + TypeScript“.
- **2 skyriaus lentelė**, Artūro eilutė: „React + JS“ → „React + TypeScript“.

Kaip naudoti TS, kad neapkrautų:
- Projektą kurti iš šablono: `npm create vite@latest web -- --template react-ts`.
- `tsconfig.json` – pradėti nuo `"strict": false`; jei viskas sekasi, vėliau galima įjungti.
- Tipus griežtai naudoti tik ten, kur jie labiausiai padeda: **API atsakymams ir WS įvykiams** iš sugeneruoto `src/api/schema.d.ts` (`npx openapi-typescript ../Synq.yaml -o src/api/schema.d.ts`). Komponentus galima rašyti beveik kaip JS.
- WS kadrams aprašyti vieną `type ServerFrame = ...` union tipą `src/realtime/` aplanke – tada `switch (frame.type)` pats parodo, kurio įvykio neapdorojai.
- Pasikeitus `Synq.yaml`, paleisti `scripts/gen-api.sh` ir `npm run build`: TS iškart parodys, kur kodas nebeatitinka kontrakto.
