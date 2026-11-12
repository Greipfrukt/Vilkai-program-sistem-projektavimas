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




### SUMMARY
# Synq paprastais žodžiais

## Kas yra Synq?

Synq – tai **internetinė programa komandoms**, kurioje galima **susirašinėti** ir **kartu rašyti tekstą**, nepereinant į kitas programas. Panašiai kaip Slack, Microsoft Teams ar Discord, tik dar su bendrais dokumentais, kuriuos keli žmonės gali rašyti vienu metu (kaip Google Docs).

**Kokią problemą tai sprendžia?** Dabar komandos dažnai kalbasi vienoje programoje, failus siunčiasi el. paštu, o dokumentus rašo dar kitur. Dėl to:
- reikia nuolat šokinėti tarp programų;
- atsiranda kelios to paties dokumento versijos ir dalis darbo pasimeta;
- neaišku, kas šiuo metu prisijungęs, ką dar perskaitei ir kas ką redaguoja.

Synq viską sudeda į vieną vietą.

## Keli žodžiai, kuriuos reikia žinoti

| Žodis | Ką reiškia |
|---|---|
| **Darbo erdvė** (workspace) | Atskira komandos „patalpa“, pvz. vienos įmonės ar studentų grupės. Vienas žmogus gali būti keliose. |
| **Kanalas** | Pokalbių „kambarys“ darbo erdvėje tam tikra tema, pvz. #marketingas. Būna **viešas** (gali prisijungti kiekvienas komandos narys) arba **privatus** (tik pakviesti). |
| **Asmeninis pokalbis** | Susirašinėjimas tarp dviejų žmonių. |
| **Grupinis pokalbis** | Susirašinėjimas tarp 3–20 žmonių, ne kanale. |
| **Savininkas** | Žmogus, sukūręs darbo erdvę. Visada būna tik vienas. |
| **Administratorius** | Savininko paskirtas „prižiūrėtojas“: kviečia žmones, kuria kanalus. |
| **Narys** | Paprastas dalyvis. |
| **Bendras dokumentas** | Tekstas kanale, kurį keli žmonės gali rašyti tuo pačiu metu. |

## Žymėjimas

- ✅ **Darom** – bus pirmoje versijoje.
- 🟡 **Jei liks laiko** – darysime, jei spėsime.
- ❌ **Atidėta** – ne šioje versijoje.

---

## 1. Paskyra ir darbo erdvės

**FR-001 – Registracija ir prisijungimas** ✅
Žmogus susikuria paskyrą su el. paštu, vardu ir slaptažodžiu (bent 8 simboliai). Tuo pačiu el. paštu antros paskyros sukurti negalima. Jei prisijungiant suklystama, sistema nesako, kas tiksliai neteisinga, kad pašaliniam būtų sunkiau atspėti.

**FR-002 – Prisijungimo sesija** ✅
Prisijungus sistema tave „atsimena“, kol neatsijungi arba kol baigiasi laikas. Atsijungus ar praėjus laikui nieko nebegali matyti, kol neprisijungsi iš naujo.

**FR-003 – Profilis** ✅
Gali pasikeisti savo rodomą vardą ir profilio nuotrauką. El. pašto pakeisti negalima.

**FR-004 – Darbo erdvės kūrimas** ✅
Bet kas gali susikurti savo darbo erdvę ir tampa jos savininku. Kiekviena darbo erdvė turi savo žmones ir kanalus, kurių kitų erdvių žmonės nemato.

**FR-005 – Kvietimai** ✅
Savininkas ar administratorius sukuria kvietimo nuorodą konkrečiam el. paštui ir ją nusiunčia žmogui pats. Nuoroda galioja 7 dienas ir tik vieną kartą. Kitu el. paštu ja pasinaudoti negalima.

**FR-006 – Rolių keitimas** ✅
Tik savininkas gali paskirti ar atimti administratoriaus teises. Savininkas visada lieka vienas.

**FR-007 – Išėjimas ar pašalinimas** ✅
Narys gali pats išeiti iš darbo erdvės. Administratorius gali pašalinti narius, o savininkas – ir administratorius. Savininkas, prieš išeidamas, turi kam nors perduoti savininko teises. Pašalintas žmogus iš karto nebemato nieko, net jei programa pas jį dar atidaryta.

**FR-008 – Naršymas** ✅
Matai visas savo darbo erdves ir gali tarp jų persijungti, kaip tarp skirtukų. Vienos erdvės informacija niekada nesimaišo su kita.

## 2. Kanalai ir žinutės

**FR-009 – Kanalai** ✅
Savininkas ir administratoriai kuria kanalus, juos pervadina ir archyvuoja. Dviejų kanalų tuo pačiu pavadinimu būti negali. Archyvuotas kanalas – kaip užrakinta spinta su stiklinėmis durimis: senas žinutes matyti galima, rašyti naujų – ne.

**FR-010 – Privatūs kanalai** ✅
Privatų kanalą mato tik ten pakviesti žmonės (ir savininkas su administratoriais). Kiti net nežino, kad toks kanalas yra.

**FR-011 – Žinutės siuntimas** ✅
Gali parašyti žinutę iki 4000 simbolių (maždaug dviejų puslapių) ir prisegti iki 5 failų. Galima siųsti ir vien failą be teksto. Žinutė neišnyksta perkrovus puslapį.

**FR-012 – Žinutės ateina iš karto** ✅
Kai kas nors parašo, kiti tą žinutę pamato iš karto, be puslapio perkrovimo. Taip yra net tada, kai žmonės prisijungę prie skirtingų mūsų serverių.

**FR-013 – Žinučių istorija ir „pasivijimas“** ✅
Gali paslinkti aukštyn ir pamatyti senesnes žinutes. Jei trumpam dingo internetas, prisijungus vėl gauni viską, ką praleidai, ir nieko du kartus.

**FR-014 – Jokių dublikatų ir teisinga tvarka** ✅
Jei internetas strigo ir programa žinutę išsiuntė dar kartą, ji vis tiek atsiras tik vieną kartą. Visi pokalbio dalyviai mato žinutes ta pačia tvarka.

**FR-015 – Asmeniniai ir grupiniai pokalbiai** ✅
Gali susirašinėti su vienu žmogumi arba su grupele (3–20 žmonių) iš tos pačios darbo erdvės. Kiti jūsų pokalbio nemato.

**FR-016 – Žinučių taisymas ir trynimas** ✅ (tik savo žinučių) / 🟡 (administratoriams – kitų žinučių)
Gali pataisyti ar ištrinti savo žinutę. Pataisyta žinutė pažymima „redaguota“, o vietoje ištrintos lieka užrašas, kad ji buvo ištrinta. Administratorius kanale gali ištrinti netinkamas kitų žinutes, bet asmeninių pokalbių neliečia.

**FR-017 – Atsakymai gijose (threads)** ❌
Galimybė atsakyti į konkrečią žinutę atskirame šoniniame lange, kad pagrindinis pokalbis neužsikimštų.

**FR-018 – Reakcijos** ✅
Gali „paspausti“ šypsenėlę ar 👍 po žinute vietoj atsakymo. Matosi, kiek žmonių sureagavo. Tos pačios šypsenėlės vienas žmogus du kartus uždėti negali.

**FR-019 – Moderavimo žurnalas** 🟡
Kai administratorius ištrina kieno nors žinutę ar archyvuoja kanalą, sistema užsirašo kas, ką ir kada padarė, kaip apsaugos kameros įrašas.

## 3. Kas prisijungęs, kas neperskaityta, failai

**FR-020 – Prisijungęs ar ne** ✅
Prie žmogaus vardo matosi taškelis, ar jis dabar prisijungęs. Jei atsidarei programą ir telefone, ir kompiuteryje, būsi „prisijungęs“, kol uždarysi abi.

**FR-021 – Neperskaitytos žinutės** ✅
Prie kiekvieno kanalo ar pokalbio matosi, kiek naujų žinučių dar neperskaitei. Perskaičius skaičius dingsta, ir tai matosi visuose tavo įrenginiuose.

**FR-022 – Paieška** ✅
Galimybė surasti seną žinutę pagal žodį. Rezultatuose rodomos tik tos žinutės, kurias tau leidžiama matyti.

**FR-023 – Pranešimai programoje** ✅
Varpelis, rodantis, kad kažkas tau parašė asmeniškai arba paminėjo tave kanale.

**FR-024 – Failų įkėlimas** ✅
Prie žinutės gali prisegti nuotrauką (PNG, JPEG), PDF ar tekstinį failą, iki 10 MB. Kitų tipų ir didesnių failų sistema nepriims.

**FR-025 – Failų apsauga** ✅
Failą atsisiųsti gali tik tas, kas mato tą pokalbį. Net jei kažkas turi seną failo nuorodą, bet buvo pašalintas iš kanalo, atsisiųsti nebegalės.

**FR-026 – Nepavykę įkėlimai** ✅
Jei failo įkėlimas nutrūko, jis pažymimas kaip nepavykęs ir negali būti atsisiųstas pusiau sugadintas.

## 4. Bendri dokumentai (pagrindinė Synq ypatybė) ❌

Įsivaizduokite popieriaus lapą ant stalo, prie kurio sėdi keli žmonės ir visi tuo pačiu metu rašo į jį savo dalis. Kiekvienas mato, ką kiti rašo, ir niekas nieko neištrina netyčia. Synq tai daro internetu.

**FR-027 – Dokumento sukūrimas** ❌
Kanalo nariai gali sukurti dokumentą su pavadinimu. Jį vėliau galima atsidaryti iš kanalo. Kitų kanalų žmonės jo nemato.

**FR-028 – Kas gali redaguoti** ❌
Redaguoti gali tik tie, kas gali rašyti tame kanale. Archyvuotame kanale dokumentą galima tik skaityti.

**FR-029 – Rašymas vienu metu** ❌
Bent trys žmonės gali rašyti tą patį dokumentą vienu metu ir mato vieni kitų pakeitimus iš karto.

**FR-030 – Jokių konfliktų** ❌
Jei du žmonės tą pačią akimirką keičia tą pačią vietą, sistema pati sujungia abu pakeitimus ir nė vieno neprarandama. Visiems galiausiai rodomas lygiai tas pats tekstas.

**FR-031 – Atsidarius matai naujausią versiją** ❌
Atsidaręs dokumentą iš karto matai naujausią tekstą ir toliau gauni kitų pakeitimus. Uždarius – nebegauni.

**FR-032 – Niekas nedingsta** ❌
Viskas, ką parašei, išsaugoma automatiškai. Net jei perkrausi puslapį ar sugrius vienas mūsų serveris, tekstas liks.

**FR-033 – Dingus internetui** ❌
Jei dingo internetas, programa parodo, kad ryšio nėra. Grįžus ryšiui ji pati atsisiunčia, ką praleidai, ir išsiunčia, ką parašei per tą laiką. Jei ko nors nepavyko išsiųsti, tai aiškiai pažymima, o ne tyliai prarandama.

**FR-034 – Matai, kas dar rašo** ❌
Dokumente matosi, kas dar jį dabar atsidaręs, ir kur kiekvienas yra su savo žymekliu (spalvotas brūkšnelis su vardu). Žmogui išėjus, jis po kelių sekundžių dingsta iš sąrašo.

**FR-035 – Apsauga nuo sugadinimo** ❌
Dokumentas gali būti iki 100 000 simbolių (maždaug 50 puslapių). Sistema tikrina kiekvieną pakeitimą, kad niekas negalėtų tyčia ar netyčia sugadinti teksto.

**FR-036 – Teksto formatavimas** ❌
Paryškintas, pasviręs tekstas, antraštės. Pirmoje versijoje – tik paprastas tekstas.

**FR-037 – Ilgas darbas be interneto** ❌
Galimybė ilgai rašyti be interneto ir vėliau viską sujungti.

**FR-038 – Dokumento pervadinimas ir trynimas** ❌
Dokumento kūrėjas, administratorius ar savininkas gali jį pervadinti arba ištrinti. Ištrynus, visiems jį atsidariusiems jis užsidaro.

**FR-039 – Versijų istorija** ❌
Galimybė pažiūrėti, kaip dokumentas atrodė anksčiau, ir sugrąžinti senesnę versiją.

---

## Santrauka

| Sritis | Ką gali daryti žmogus | Pirmoje versijoje |
|---|---|---|
| Paskyra | Užsiregistruoti, prisijungti, pasikeisti vardą ir nuotrauką | ✅ |
| Komanda | Sukurti darbo erdvę, pakviesti žmones, paskirti administratorius | ✅ |
| Pokalbiai | Rašyti kanaluose, asmeniškai ir grupėse; prisegti failus | ✅ |
| Patogumai | Taisyti ir trinti žinutes, reaguoti šypsenėlėmis | ✅ |
| Būsena | Matyti, kas prisijungęs ir kiek neperskaitytų žinučių | ✅ |
| Dokumentai | Rašyti tą patį tekstą keliese vienu metu, nieko neprarandant | ❌ |
| Vėliau | Gijos, paieška, pranešimai, formatavimas, versijų istorija | ❌ |

**Ko Synq pirmoje versijoje nedarys:** skambučių ir vaizdo pokalbių, programėlių telefonui, el. laiškų pranešimų, piešimo lentų, mokamų planų ir dirbtinio intelekto funkcijų.