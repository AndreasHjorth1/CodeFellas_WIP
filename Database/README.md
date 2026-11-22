# Database — mappeoppgave 2

SQL-skriptene for databasedelen. De står utenfor applikasjonen med vilje: appen
bruker EF Core-migreringene i `Prosjekt/Prosjekt/Migrations`, og de skriptene her
endrer ingenting på den. Se notatet til gruppa for begrunnelsen.

## Kjørerekkefølge

| Fil | Innhold |
|---|---|
| `01_schema.sql` | Tabeller, datatyper, primær- og fremmednøkler. Oppslagstabellene fylles her |
| `02_constraints_views.sql` | CHECK-constraints, indekser og tre views |
| `03_testdata.sql` | 12 brukere, 118 ressurser, 28 behov, 18 tildelinger, 20 varsler, 25 loggrader |
| `04_queries.sql` | De sju spørringene oppgaven ber om |

Kjør dem i rekkefølge mot en **egen** database, ikke mot `Mapdb` som appen bruker.

```powershell
docker ps   # finn MariaDB-containerens id
docker exec -i <id> mariadb -u root -p<PASSORD> -e "CREATE DATABASE IF NOT EXISTS Mapdb_eksamen CHARACTER SET utf8mb4;"

Get-Content .\01_schema.sql           -Raw -Encoding UTF8 | docker exec -i <id> mariadb -u root -p<PASSORD> --default-character-set=utf8mb4 Mapdb_eksamen
Get-Content .\02_constraints_views.sql -Raw -Encoding UTF8 | docker exec -i <id> mariadb -u root -p<PASSORD> --default-character-set=utf8mb4 Mapdb_eksamen
Get-Content .\03_testdata.sql         -Raw -Encoding UTF8 | docker exec -i <id> mariadb -u root -p<PASSORD> --default-character-set=utf8mb4 Mapdb_eksamen
```

Passordet står som `MYSQL_ROOT_PASSWORD` i Aspire-dashboardet, eller:
`docker inspect <id> | Select-String MYSQL_ROOT_PASSWORD`

## Antagelser som må stå i besvarelsen

* **Passordhash.** Brukerne i testdataene har en plassholder som `PasswordHash`. De finnes
  for at spørringene skal gi mening, ikke for innlogging i applikasjonen.
* **Identity-tabeller.** `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens` og
  `AspNetRoleClaims` er utelatt. De er rammeverksplumbing og ingen av spørringene rører dem.
* **Kategorier.** `Category` er en tekstkolonne med fremmednøkkel til en oppslagstabell, ikke
  en heltalls-ID. Det holder spørringene og testdataene lesbare, og matcher enum-navnene i C#.
* **Tildelinger bruker `RESTRICT`.** En tildeling skal ikke forsvinne stille fordi noen sletter
  et behov eller en ressurs. Dette er også utgangspunktet for slettediskusjonen i oppgaven.
* **Datoer.** Ressursene er lagt ut over 2023–2026 for at spørringene om år og måned skal gi
  et meningsfylt svar. Applikasjonens egen seeder bruker datoer nær dagens dato.

## Kjent avvik fra applikasjonen

EF Core-modellen har i dag **ingen** fremmednøkler på `Allocations`, `Notifications` eller
`AuditLogs`. Skriptene her legger dem inn. Skal appen følge etter, må slettekoden i
`NeedController` og `ResourceController` slette tildelinger *før* forelderraden.
