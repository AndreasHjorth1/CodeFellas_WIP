-- 02_constraints_views.sql — CHECK-constraints, indekser og views (Beredt, mappeoppgave 2)
-- Kjøres etter 01_schema.sql og før 03_testdata.sql.

-- ===== CHECK-constraints =====
-- Regler databasen kan håndheve selv. Appen validerer de samme reglene i skjemaene,
-- men validering i applikasjonslaget hjelper ikke mot en feil i et skript eller en
-- direkte INSERT. Alternativ vi valgte bort: triggere. De kan uttrykke mer, men er
-- vanskeligere å lese og feilsøke, og vi trenger ikke mer enn CHECK her.

ALTER TABLE Resources
  ADD CONSTRAINT CK_Resources_Quantity  CHECK (Quantity >= 0),
  ADD CONSTRAINT CK_Resources_Period    CHECK (AvailableTo >= AvailableFrom),
  ADD CONSTRAINT CK_Resources_Latitude  CHECK (Latitude  BETWEEN -90  AND 90),
  ADD CONSTRAINT CK_Resources_Longitude CHECK (Longitude BETWEEN -180 AND 180);

ALTER TABLE Needs
  ADD CONSTRAINT CK_Needs_Status    CHECK (Status BETWEEN 0 AND 3),
  ADD CONSTRAINT CK_Needs_Latitude  CHECK (Latitude  BETWEEN -90  AND 90),
  ADD CONSTRAINT CK_Needs_Longitude CHECK (Longitude BETWEEN -180 AND 180),
  ADD CONSTRAINT CK_Needs_Area      CHECK (AreaSquareMeters IS NULL OR AreaSquareMeters >= 0);

-- En tildeling på null enheter er meningsløs.
ALTER TABLE Allocations
  ADD CONSTRAINT CK_Allocations_Quantity CHECK (Quantity > 0);

ALTER TABLE AuditLogs
  ADD CONSTRAINT CK_AuditLogs_Action CHECK (Action IN ('Create','Update','Delete'));

-- ===== Indekser =====
-- Fremmednøklene får indeks automatisk i InnoDB. Disse er lagt til fordi spørringene
-- i 04 grupperer og filtrerer på dem.
CREATE INDEX IX_Resources_CreatedAt     ON Resources (CreatedAt);
CREATE INDEX IX_Resources_Category      ON Resources (Category);
CREATE INDEX IX_Resources_User_Created  ON Resources (UserId, CreatedAt);
CREATE INDEX IX_Needs_CreatedAt         ON Needs (CreatedAt);
CREATE INDEX IX_Needs_Status            ON Needs (Status);
CREATE INDEX IX_Notifications_User_Read ON Notifications (UserId, IsRead);
CREATE INDEX IX_AuditLogs_Entity        ON AuditLogs (EntityType, EntityId);

-- IX_Resources_User_Created er sammensatt: spørring 5 filtrerer på år og grupperer
-- på bruker, og da kan databasen lese begge kolonnene fra samme indeks.

-- ===== Views =====

-- Ressurser per år og kategori. Brukes av flere av spørringene i 04.
CREATE OR REPLACE VIEW vw_ResourceStats AS
SELECT YEAR(r.CreatedAt)  AS Aar,
       MONTH(r.CreatedAt) AS Maaned,
       r.Category,
       COUNT(*)           AS Antall,
       SUM(r.Quantity)    AS SumAntallEnheter
FROM Resources r
GROUP BY YEAR(r.CreatedAt), MONTH(r.CreatedAt), r.Category;

-- Hvor mye av hver ressurs som er bundet opp i tildelinger.
-- Dette er den samme regelen som Available => Quantity - Allocated i applikasjonen,
-- uttrykt én gang i databasen i stedet for i hver spørring.
CREATE OR REPLACE VIEW vw_ResourceAvailability AS
SELECT r.Id,
       r.Name,
       r.Category,
       r.Quantity                              AS Totalt,
       COALESCE(SUM(a.Quantity), 0)            AS Tildelt,
       r.Quantity - COALESCE(SUM(a.Quantity),0) AS Ledig
FROM Resources r
LEFT JOIN Allocations a ON a.ResourceId = r.Id
GROUP BY r.Id, r.Name, r.Category, r.Quantity;

-- Behov med antall tildelinger, for koordinatorens oversikt.
CREATE OR REPLACE VIEW vw_NeedCoverage AS
SELECT n.Id,
       n.Name,
       n.Category,
       n.Priority,
       n.Status,
       COUNT(a.Id)                  AS AntallTildelinger,
       COALESCE(SUM(a.Quantity), 0) AS SumTildelt
FROM Needs n
LEFT JOIN Allocations a ON a.NeedId = n.Id
GROUP BY n.Id, n.Name, n.Category, n.Priority, n.Status;
