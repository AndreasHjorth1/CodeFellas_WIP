# Prosjekt Kartverket/Heimevernet

Prosjekt/løsning laget av gruppe 12, IS-202 Programmeringsprosjekt, UiA høsten 2026.

Webløsning for kriseberedskap i Totalforsvaret. Offentlige aktører registrerer behov, ressursleverandører (privatpersoner, bedrifter, frivillige) registrerer ressurser, og kriseledelsen matcher ressurser mot behov i kart og følger statusen til behovet er løst.

### Gruppen består av:
Emil Rode - emilro@uia.no,
Andreas Hjorth - andreasnh@uia.no,
Sigve Sørensen - sigves@uia.no,
Leart Hasanaj - learth@uia.no,
Aryan Sarvarsen - aryans@uia.no,
Bjørn Tore Busk - bjorntb@uia.no

**Innhold:** [Drift](#drift) · [Systemarkitektur](#systemarkitektur) · [Testing](#testing) · [Arbeidsflyt](#arbeidsflyt) · [Kjente begrensninger](#kjente-begrensninger) · [Bruk av KI](#bruk-av-ki)

---

## Drift

### Krav
- .NET SDK 10
- Docker Desktop (MariaDB kjører som container)

### Kjøre lokalt
Fra rotmappa i repoet:
```
dotnet run --project Prosjekt/Prosjekt.AppHost
```

Aspire-dashboardet åpnes i nettleseren og viser disse ressursene:

| Ressurs | Hva | Adresse |
|---|---|---|
| `mariadbcontainer` | MariaDB-server, data i Docker-volum | Port tildeles av Aspire |
| `Mapdb` | Databasen appen bruker | – |
| `Prosjekt` | Webappen, bygget fra `Prosjekt/Prosjekt/Dockerfile` | http://localhost:5027 (https://localhost:5028 når sertifikat finnes) |

### Ved oppstart
Appen kjører `Database.Migrate()`, oppretter rollen `Admin` og to testbrukere:

| Bruker | Rolle |
|---|---|
| `admin@heimevernet.local` | `Admin` – offentlig aktør |
| `user@heimevernet.local` | `Bruker` – ressursleverandør |

Passord settes via **Glemt passord**. Appen sender ingen ekte e-post: `ConsoleEmailSender` skriver tilbakestillingslenken til loggen til `Prosjekt` i Aspire-dashboardet.

### Konfigurasjon og hemmeligheter
| Innstilling | Kilde |
|---|---|
| `ConnectionStrings:Mapdb` | Sendes inn av Aspire (`WithReference`). Appen stopper med feilmelding hvis den mangler |
| Databasepassord | Generert av Aspire, lagret i user-secrets for `Prosjekt.AppHost` |
| HTTPS-sertifikat (valgfritt) | `~/.aspnet/https/prosjekt.pfx`, passord i user-secrets (`Parameters:https-passord`). Se kommentar i `AppHost.cs` |
| Design-time (`dotnet ef`) | `ApplicationDbContextFactory`: `ConnectionStrings__Mapdb`, ellers `localhost` uten passord |

Ingen passord eller hemmeligheter ligger i repoet.

### Database og migrasjoner
Ny migrasjon (fra mappa `Prosjekt`):
```
dotnet ef migrations add <Navn> --project Prosjekt
```
Den kjøres automatisk ved neste oppstart.

**Nullstille databasen**, også når `mariadbcontainer` er *Unhealthy* med `Access denied for user 'root'` (volumet er laget med et eldre passord):
```
docker volume ls --filter name=mariadbcontainer
docker volume rm <navn>
```

### Logger og helse
- Logger, status og sporing for alle ressurser vises i Aspire-dashboardet (via `Prosjekt.ServiceDefaults`).
- I Development svarer webappen på `/health` og `/alive`.
- Utenfor Development vises en generell feilside (`/Home/Error`), og HSTS og HTTPS-redirect er på.

### Kontinuerlig integrasjon
`.github/workflows/build-test.yml` kjører på hver PR og push mot `main`: restore, bygg (Release), tester med kodedekning (coverlet, `.cshtml` utelatt), og publiserer testresultater og dekning som kommentar på PR-en.

---

## Systemarkitektur

Appen er laget i ASP.NET Core MVC (.NET 10) med Entity Framework Core og MariaDB, og startes via .NET Aspire.

### Prosjekter i løsningen

| Prosjekt | Ansvar |
|---|---|
| `Prosjekt` | Selve webappen: kontrollere, views, modeller, datatilgang og tjenester |
| `Prosjekt.AppHost` | Aspire-orkestrering. Starter MariaDB-containeren og appen (bygget fra `Dockerfile`), og sender tilkoblingsstrengen inn som `ConnectionStrings:Mapdb` |
| `Prosjekt.ServiceDefaults` | Felles Aspire-oppsett: helsesjekker, logging og telemetri |
| `Prosjekt.Xunit.Test` | Enhetstester for kontrollere, modeller, tjenester og repositorier (xUnit v3) |

### Lagdeling

```text
Nettleser ──▶ Controller ──▶ Repository (interface) ──▶ ApplicationDbContext (EF Core) ──▶ MariaDB
                  │
                  └──▶ Views (Razor + Leaflet/JavaScript)
```

- **Kontrollere** tar imot forespørsler, validerer input og sjekker tilgang. De snakker aldri direkte med databasen.
- **Repositorier** (`INeedRepository`, `IResourceRepository`, `IAllocationRepository`) skjuler EF Core bak et interface. Implementasjonen velges i `Program.cs` via `DataAccess:Provider`, og testene bruker egne fake-implementasjoner.
- **ViewModels** skiller det skjemaene viser fra det som lagres. Kategorier og typer ligger som enums i viewmodellene og lagres som tekst i databasen.

### Kontrollere

| Kontroller | Tilgang | Ansvar |
|---|---|---|
| `HomeController` | Åpen. `Index` (kart) krever innlogging | Forside, kart, personvern, om oss, brukerinstruks, kriseinformasjon og feilside |
| `NeedController` | Kun Admin. `GetInformation` er åpen for alle innloggede (telefonnummer skjules for andre enn Admin) | Opprette, redigere og slette behov med viktighet, frist, status og posisjon eller område i kart. Matching mot ressurser (`Match`), tildeling og fjerning av tildeling (`Allocate`/`Deallocate`), markere løst/uløst |
| `ResourceController` | Innloggede brukere | Opprette, redigere og slette ressurser med kategori, undertype, antall, tidsrom og posisjon. Vanlige brukere ser og endrer bare egne ressurser, mens Admin ser alle. Eksport til CSV og JSON er kun for Admin |
| `AccountController` | Åpen | Registrering, innlogging (lockout etter 10 feilforsøk), utlogging, tilgang nektet, glemt passord og tilbakestilling av passord |
| `SettingsController` | — | Placeholder-side, ikke implementert ennå |

### Roller og tilgang

| | Offentlig aktør (`Admin`) | Ressursleverandør (Bruker) | Ikke innlogget |
|---|---|---|---|
| Forside, om oss, personvern, brukerinstruks, kriseinformasjon | ✓ | ✓ | ✓ |
| Kart med behov og ressurser | ✓ | ✓ (uten telefonnummer på behov, bare egne ressurser) | – |
| Registrere og endre behov, matche, tildele | ✓ | – | – |
| Registrere ressurser | ✓ | ✓ | – |
| Se, endre, slette ressurser | Alle | Egne | – |
| Eksport CSV/JSON | ✓ | – | – |

### Datamodell

| Tabell | Innhold |
|---|---|
| `Needs` | Behov: navn, beskrivelse, kategori, telefon, viktighet (akutt/planlagt), frist, status, koordinater og eventuelt tegnet område (GeoJSON med areal og omkrets) |
| `Resources` | Ressurser: navn, beskrivelse, kategori og undertype, antall, tilgjengelighet, tidsrom, koordinater og eier (`UserId`, fremmednøkkel til Identity-bruker) |
| `Allocations` | Kobler et behov til en ressurs med et antall. Ledig antall på en ressurs regnes ut, det lagres ikke |
| `AspNet*` | ASP.NET Core Identity: brukere, roller og innloggingsdata |

Koordinater lagres som en `Coordinates`-verdi (WGS84 / EPSG:4326), mappet til kolonnene `Latitude` og `Longitude`.

### Sentral logikk

- **Statusflyt:** Ny → Vurderes → Tildelt → Løst. `EntryStatusFlow` tillater bare ett steg om gangen, fremover eller tilbake. Regelen sjekkes på serveren i `NeedController.Edit`.
- **Matching:** `CategoryMatching` oversetter behovstype til aktuelle ressurskategorier, for eksempel Snørydding → Kjøretøy og Verktøy. `Distance.InMeters` (Haversine) sorterer kandidatene etter avstand til behovet.
- **Eierskap:** En ressurs får innlogget brukers `UserId` ved opprettelse.
- **Tidssone:** `NorwegianTime` gir norsk tid uavhengig av om serveren kjører på Windows eller Linux.

### Kart

Kartene er laget med Leaflet og Leaflet.markercluster.

- Klikker du i kartet, fylles koordinatene ut automatisk. Behov kan også ha et tegnet område.
- Kartdata hentes som JSON fra `GetInformation`-endepunktene.

| Ekstern tjeneste | Brukes til |
|---|---|
| OpenStreetMap | Standard bakgrunnskart |
| Kartverket (cache.kartverket.no) | Topografisk bakgrunnskart |
| Esri World Imagery | Flybilder (midlertidig, til Geonorge-tilgang er på plass) |
| Nominatim (OpenStreetMap) | Adresse fra koordinater og stedssøk |
| cdnjs | Leaflet.draw og Font Awesome |

### Sikkerhet

- **Identity:** ASP.NET Core Identity med roller. Tilgang styres med `[Authorize]` og `[Authorize(Roles = Admin)]` på kontroller- og action-nivå.
- **CSRF:** Antiforgery-token valideres globalt på alle POST-forespørsler.
- **Headere:** `SecurityHeaders` setter Content-Security-Policy, `X-Content-Type-Options`, `X-Frame-Options` og `Referrer-Policy`. Kestrel skjuler `Server`-headeren og begrenser forespørsler til 30 MB.
- **HTTPS:** HTTPS-endepunkt med eget utviklersertifikat når sertifikatet finnes. Utenfor Development brukes HSTS og omdirigering til HTTPS.
- **Hemmeligheter:** Tilkoblingsstreng og passord kommer bare fra Aspire og user-secrets, aldri fra git.
- **E-post:** Appen sender ikke e-post ennå. `ConsoleEmailSender` skriver lenker for tilbakestilling av passord til loggen.

---

## Testing

### Automatiske tester
Kjøres fra mappa `Prosjekt` med `dotnet test`, og i CI på hver PR.

| Testklasse | Testes | Antall |
|---|---|---|
| `NeedControllerTest` | Opprettelse og validering (koordinater, tomme navn, flere behov), redigering, statusflyt (hoppe over steg, ett steg tilbake), sletting, `GetInformation` per rolle (Admin ser telefon, vanlig bruker gjør ikke, ikke innlogget avvises) | 24 |
| `ResourceControllerTest` | Opprettelse og validering, redigering, sletting, kjøretøy (reg.nr.) og klær (størrelse), eksport CSV/JSON (filtrering, sortering, semikolon i tekst) | 30 |
| `NeedRepositoryTest`, `ResourceRepositoryTest` | CRUD mot SQLite i minnet | 16 |
| `EntryStatusFlowTest` | Regler for statusflyt | 4 |
| `SecurityHeadersTest` | At sikkerhetsheaderne settes på svarene | 2 |
| `HomeControllerTest`, `InnstillingerControllerTest` | Visninger og feilside | 4 |
| **Totalt** | | **132** |

Controllertestene bruker falske repositories (`Fakes/`). Repository-testene bruker ekte EF Core mot SQLite, så spørringer og lagring testes ende til ende.

### Resultater
| Kjøring | Resultat |
|---|---|
| Siste CI-kjøring på `main` | **(fyll inn: antall grønne / totalt)** |
| Kodedekning (`Prosjekt`, uten views) | **(fyll inn fra PR-kommentaren)** |

### Manuelle testscenarier
| # | Scenario | Forventet | Resultat |
|---|---|---|---|
| 1 | Start via AppHost på en ren maskin | Ingen passordspørsmål, alle ressurser grønne | **(fyll inn)** |
| 2 | Glemt passord for `admin@heimevernet.local` | Lenke i loggen, nytt passord virker | **(fyll inn)** |
| 3 | 10 feil passord på rad | Kontoen låses | **(fyll inn)** |
| 4 | Behov/ressurs med tomme eller ugyldige felt | Feilmelding, ingen krasj | **(fyll inn)** |
| 5 | Ressursleverandør registrerer ressurs | Vises i «Mine ressurser» og på kartet | **(fyll inn)** |
| 6 | Ressursleverandør prøver å endre en annens ressurs | Nektes | **(fyll inn)** |
| 7 | Admin registrerer behov med tegnet område | Område og areal vises i kartet | **(fyll inn)** |
| 8 | Matching for et transportbehov | Kjøretøy listes, nærmeste først | **(fyll inn)** |
| 9 | Statusendring Ny → Løst direkte | Avvises med feilmelding | **(fyll inn)** |
| 10 | Eksport CSV/JSON | Fil lastes ned, æøå og semikolon er riktige | **(fyll inn)** |
| 11 | Stopp og start Aspire | Data er fortsatt der | **(fyll inn)** |
| 12 | Ukjent URL | **(fyll inn)** | **(fyll inn)** |

---

## Arbeidsflyt
- Hver lager sin egen branch per endring: `Navn/hva-du-gjør`.
- Pull request mot `main`, godkjent av minst én annen før merge. CI må være grønn.
- Merge-konflikter i `Program.cs`: sammenlign alle `AddScoped`-registreringer på begge sider før du velger.

## Kjente begrensninger
- Ingen egen 404-side.
- Ingen ekte e-postsending (lenker skrives til loggen).
- Ingen tofaktorautentisering. Etter avtale med foreleser inngår det ikke i faget.
- Innstillinger-siden er bare en placeholder.
- Medisinbehov matches mot provisjon, siden det ikke finnes en egen kategori for medisinsk utstyr. Ressurskategorien Klær matches ikke av noen behovstype.
- MariaDB-imaget er `latest` og bør låses til en versjon.

---

## Bruk av KI

Vi har brukt KI som et hjelpemiddel gjennom programmeringsprosessen. KI har blant annet blitt brukt til å forklare kode og feilmeldinger, finne og rette feil, foreslå løsninger og hjelpe med implementering av funksjonalitet. KI har vært et støtteverktøy, mens vi selv har vurdert, tilpasset, bygget og testet løsningene i prosjektet.

### Verktøy
| Verktøy | Brukt til |
|---|---|
| Claude (Anthropic), Gemini (Google), Chat-GTP (OpenAI) | Kodegjennomgang, sikkerhetsgjennomgang, sammenligning med foreleserens repo, feilsøking, merge-konflikter, tester, dokumentasjon |
| GitHub Copilot | Forslag i editoren, og Copilot coding agent på enkelte PR-er (commits fra `copilot-swe-agent[bot]`) |

`.github/copilot-instructions.md` (fra foreleserens repo) styrer Copilot til å forklare og la oss forstå koden før den committes.
