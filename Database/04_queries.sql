-- 04_queries.sql — de sju spørringene (Beredt, mappeoppgave 2)
-- Kjøres etter 01, 02 og 03. Hver spørring kan kjøres for seg.

-- =====================================================================
-- 1. De fem første radene av de viktigste tabellene, sortert
-- =====================================================================
-- Nyeste først, fordi det er rekkefølgen en koordinator jobber i.

-- 1a. Ressurser
SELECT Id, Name, Category, Quantity, IsAvailable, CreatedAt
FROM Resources
ORDER BY CreatedAt DESC, Id DESC
LIMIT 5;

-- 1b. Behov
SELECT Id, Name, Category, Priority, Status, CreatedAt
FROM Needs
ORDER BY CreatedAt DESC, Id DESC
LIMIT 5;

-- 1c. Tildelinger, med navn fra begge sider
SELECT a.Id, n.Name AS Behov, r.Name AS Ressurs, a.Quantity, a.CreatedAt
FROM Allocations a
JOIN Needs n     ON n.Id = a.NeedId
JOIN Resources r ON r.Id = a.ResourceId
ORDER BY a.CreatedAt DESC, a.Id DESC
LIMIT 5;

-- 1d. Brukere, flest ressurser først
SELECT u.FullName, u.Email, COUNT(r.Id) AS AntallRessurser
FROM AspNetUsers u
LEFT JOIN Resources r ON r.UserId = u.Id
GROUP BY u.Id, u.FullName, u.Email
ORDER BY AntallRessurser DESC, u.FullName
LIMIT 5;


-- =====================================================================
-- 2. Brukere med mer enn fem ressurser, sortert etter antall
-- =====================================================================
-- INNER JOIN: en bruker uten ressurser kan uansett ikke ha mer enn fem.
SELECT u.FullName, u.Email, COUNT(r.Id) AS AntallRessurser
FROM AspNetUsers u
JOIN Resources r ON r.UserId = u.Id
GROUP BY u.Id, u.FullName, u.Email
HAVING COUNT(r.Id) > 5
ORDER BY AntallRessurser DESC, u.FullName;


-- =====================================================================
-- 3. Totalt antall ressurser registrert per år
-- =====================================================================
SELECT YEAR(CreatedAt) AS Aar, COUNT(*) AS AntallRessurser
FROM Resources
GROUP BY YEAR(CreatedAt)
ORDER BY Aar;


-- =====================================================================
-- 4. Totalt antall ressurser registrert per måned i 2026
-- =====================================================================
SELECT MONTH(CreatedAt)                   AS Maaned,
       MONTHNAME(CreatedAt)               AS MaanedNavn,
       COUNT(*)                           AS AntallRessurser
FROM Resources
WHERE YEAR(CreatedAt) = 2026
GROUP BY MONTH(CreatedAt), MONTHNAME(CreatedAt)
ORDER BY Maaned;


-- =====================================================================
-- 5. Den som har lagt inn flest ressurser i 2025
-- =====================================================================
-- LIMIT 1 gir én rad. Ved uavgjort plukker databasen vilkårlig; varianten
-- under spørringen håndterer deling av førsteplassen.
SELECT u.FullName, u.Email, COUNT(*) AS AntallI2025
FROM Resources r
JOIN AspNetUsers u ON u.Id = r.UserId
WHERE YEAR(r.CreatedAt) = 2025
GROUP BY u.Id, u.FullName, u.Email
ORDER BY AntallI2025 DESC
LIMIT 1;

-- 5b. Samme spørsmål, men alle som deler førsteplassen kommer med.
SELECT u.FullName, u.Email, COUNT(*) AS AntallI2025
FROM Resources r
JOIN AspNetUsers u ON u.Id = r.UserId
WHERE YEAR(r.CreatedAt) = 2025
GROUP BY u.Id, u.FullName, u.Email
HAVING COUNT(*) = (
    SELECT MAX(Antall) FROM (
        SELECT COUNT(*) AS Antall
        FROM Resources
        WHERE YEAR(CreatedAt) = 2025 AND UserId IS NOT NULL
        GROUP BY UserId
    ) AS t
);


-- =====================================================================
-- 6. Ressurstyper med flest registreringer (topp 3)
-- =====================================================================
SELECT rc.DisplayName AS Kategori, COUNT(*) AS AntallRessurser
FROM Resources r
JOIN ResourceCategories rc ON rc.Code = r.Category
GROUP BY r.Category, rc.DisplayName
ORDER BY AntallRessurser DESC
LIMIT 3;


-- =====================================================================
-- 7. År med flere registrerte ressurser enn 2026
-- =====================================================================
SELECT YEAR(CreatedAt) AS Aar, COUNT(*) AS AntallRessurser
FROM Resources
GROUP BY YEAR(CreatedAt)
HAVING COUNT(*) > (SELECT COUNT(*) FROM Resources WHERE YEAR(CreatedAt) = 2026)
ORDER BY AntallRessurser DESC;


-- =====================================================================
-- Bonus: viewene fra 02 i bruk
-- =====================================================================
SELECT * FROM vw_ResourceAvailability WHERE Ledig < Totalt ORDER BY Ledig LIMIT 10;
SELECT * FROM vw_NeedCoverage WHERE AntallTildelinger > 0 ORDER BY SumTildelt DESC LIMIT 10;
