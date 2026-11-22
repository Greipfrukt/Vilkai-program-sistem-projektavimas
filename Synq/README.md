# Synq

Realaus laiko komandinių pokalbių sistema (Synq V1). Kontraktas – [`Synq.yaml`](Synq.yaml), darbų planas – [`docs/Synq_darbu_planas.pdf`](docs/Synq_darbu_planas.pdf), repozitorijos medis – [`docs/Synq_repo_medis.md`](docs/Synq_repo_medis.md).

Komanda: **Pijus** (infra, Auth, Workspace), **Rokas** (Messaging, Presence), **Artūras** (web, Search, Notification), **Augustinas** (iOS).

## Paleidimas (≈10 min)

Reikia: Docker Desktop, .NET 8 SDK (backend kūrimui), Node 22 (web).

```sh
cp .env.example .env
docker compose up --build -d
docker compose ps          # visi servisai turi būti „healthy“
```

Gateway: <http://localhost:8080> (vienintelis portas, kurį naudoja web ir iOS). RabbitMQ UI: <http://localhost:15672>.

### Web

```sh
cd web
npm install
npm run mock     # 1 savaitė: Prism mock iš Synq.yaml (:4010)
npm run dev      # http://localhost:5173
```

`web/.env.development`: `VITE_API_TARGET=mock` (Prism) arba `backend` (gateway :8080).
Pasikeitus `Synq.yaml`: `npm run gen:api && npm run build`.

### Backend

```sh
cd backend
dotnet build Synq.sln
dotnet test Synq.sln      # integraciniams testams reikia veikiančio Docker
```

### iOS

Žr. [`ios/README.md`](ios/README.md). `SynqKit` testuojamas be Mac: `cd ios/SynqKit && swift test`.

## Architektūra

| Servisas | Kas | Užduotys | DB schema |
|---|---|---|---|
| `gateway` | YARP, sesijų tikrinimas, limitai | P-04 | – |
| `auth` | registracija, sesijos, profiliai | P-05 | `auth` |
| `workspace` | workspace'ai, nariai, kvietimai, kanalai, teisės | P-06 – P-09 | `workspace` |
| `messaging-1`, `messaging-2` | žinutės, WS, gijos, reakcijos | R-01 – R-08 | `messaging` |
| `presence` | online būsena (Redis) | R-09 | – |
| `search` | paieška (SHOULD) | A-07 | `search` |
| `notification` | pranešimai (SHOULD) | A-08 | `notification` |

Infrastruktūra: PostgreSQL (`:5432`), RabbitMQ (`:5672`, UI `:15672`), Redis (`:6379`).
Sprendimai – [`docs/adr/`](docs/adr).

## Taisyklės

- **`Synq.yaml` keičiamas tik PR'e kartu su kodu**; po to abu klientai iš naujo sugeneruoja tipus / modelius.
- **`AccessRules.cs` (Workspace) – vienintelė vieta teisių taisyklėms.** Kiti servisai klausia `/internal/v1/access`.
- Kiekvienas servisas rašo tik į savo DB schemą.
- Šakos: `feature/<ID>-trumpas-aprasas` (pvz. `feature/R-02-send-message`); PR su bent viena peržiūra.
- Ketvirtadienį – integracija: visi paleidžia `main` per `docker compose up`.

## Demo scenarijus

Žr. [`tests/demo/`](tests/demo) (I-09).
