-- 01_schema.sql — tabeller, datatyper, primær- og fremmednøkler (Beredt, mappeoppgave 2)
-- MariaDB / MySQL. Kjør først, deretter 02, 03, 04.
--
-- ANTAGELSER:
--  * Identity-tabellene som ikke brukes av spørringene (AspNetUserClaims, AspNetUserLogins,
--    AspNetUserTokens, AspNetRoleClaims) er utelatt. De er rammeverksplumbing, ikke domenedata.
--  * Kategoriene er egne oppslagstabeller med tekstnøkkel, så de matcher enum-navnene i C#.
--    Alternativ vi valgte bort: heltalls-ID som fremmednøkkel. Det er mer normalisert, men gjør
--    alle spørringer og all testdata uleselig uten en join, og krever en migrering av appen.

DROP TABLE IF EXISTS Allocations, Notifications, AuditLogs, Resources, Needs;
DROP TABLE IF EXISTS ResourceCategories, NeedCategories, Priorities;
DROP TABLE IF EXISTS AspNetUserRoles, AspNetRoles, AspNetUsers;

-- ===== Brukere og roller =====
CREATE TABLE AspNetUsers (
    Id                   VARCHAR(255) NOT NULL,
    UserName             VARCHAR(256),
    NormalizedUserName   VARCHAR(256),
    Email                VARCHAR(256),
    NormalizedEmail      VARCHAR(256),
    EmailConfirmed       TINYINT(1)   NOT NULL DEFAULT 0,
    PasswordHash         LONGTEXT,
    SecurityStamp        LONGTEXT,
    ConcurrencyStamp     LONGTEXT,
    PhoneNumber          LONGTEXT,
    PhoneNumberConfirmed TINYINT(1)   NOT NULL DEFAULT 0,
    TwoFactorEnabled     TINYINT(1)   NOT NULL DEFAULT 0,
    LockoutEnd           DATETIME(6)  NULL,
    LockoutEnabled       TINYINT(1)   NOT NULL DEFAULT 1,
    AccessFailedCount    INT          NOT NULL DEFAULT 0,
    FullName             VARCHAR(100) NOT NULL,
    CONSTRAINT PK_AspNetUsers PRIMARY KEY (Id),
    UNIQUE KEY UX_AspNetUsers_NormalizedEmail (NormalizedEmail)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE AspNetRoles (
    Id               VARCHAR(255) NOT NULL,
    Name             VARCHAR(256),
    NormalizedName   VARCHAR(256),
    ConcurrencyStamp LONGTEXT,
    CONSTRAINT PK_AspNetRoles PRIMARY KEY (Id),
    UNIQUE KEY UX_AspNetRoles_NormalizedName (NormalizedName)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE AspNetUserRoles (
    UserId VARCHAR(255) NOT NULL,
    RoleId VARCHAR(255) NOT NULL,
    CONSTRAINT PK_AspNetUserRoles PRIMARY KEY (UserId, RoleId),
    CONSTRAINT FK_UserRoles_User FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE,
    CONSTRAINT FK_UserRoles_Role FOREIGN KEY (RoleId) REFERENCES AspNetRoles(Id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ===== Oppslagstabeller =====
-- Unntatt tikravet i oppgaveteksten: statiske kategorilister.
CREATE TABLE ResourceCategories (
    Code        VARCHAR(100) NOT NULL,
    DisplayName VARCHAR(100) NOT NULL,
    CONSTRAINT PK_ResourceCategories PRIMARY KEY (Code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE NeedCategories (
    Code        VARCHAR(50)  NOT NULL,
    DisplayName VARCHAR(100) NOT NULL,
    CONSTRAINT PK_NeedCategories PRIMARY KEY (Code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE Priorities (
    Code        VARCHAR(50)  NOT NULL,
    DisplayName VARCHAR(100) NOT NULL,
    CONSTRAINT PK_Priorities PRIMARY KEY (Code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

INSERT INTO ResourceCategories (Code, DisplayName) VALUES
  ('Other','Annet'),('Provision','Provisjon'),('Clothing','Klaer'),('Tool','Verktoy'),
  ('Vehicle','Kjoretoy'),('Materials','Materialer'),('Competence','Kompetanse');

INSERT INTO NeedCategories (Code, DisplayName) VALUES
  ('Other','Annet'),('Food','Mat'),('Medicine','Medisin'),('Transport','Transport'),
  ('PowerGenerator','Stroemaggregat'),('SnowRemoval','Snoerydding'),('SandGravel','Sand og grus'),
  ('Machinery','Maskiner'),('Evacuation','Evakuering'),('Communication','Samband'),('Personnel','Mannskap');

INSERT INTO Priorities (Code, DisplayName) VALUES ('Acute','Akutt'),('Planned','Planlagt');

-- ===== Ressurser =====
CREATE TABLE Resources (
    Id            INT           NOT NULL AUTO_INCREMENT,
    Name          VARCHAR(200)  NOT NULL,
    Description   VARCHAR(4000) NOT NULL,
    IsAvailable   TINYINT(1)    NOT NULL DEFAULT 1,
    Category      VARCHAR(100)  NOT NULL,
    SubCategory   VARCHAR(100)  NULL,
    PhoneNumber   VARCHAR(20)   NOT NULL,
    Quantity      INT           NOT NULL DEFAULT 1,
    AvailableFrom DATETIME(6)   NOT NULL,
    AvailableTo   DATETIME(6)   NOT NULL,
    CreatedAt     DATETIME(6)   NOT NULL,
    Latitude      DOUBLE        NOT NULL,
    Longitude     DOUBLE        NOT NULL,
    UserId        VARCHAR(255)  NULL,
    CONSTRAINT PK_Resources PRIMARY KEY (Id),
    CONSTRAINT FK_Resources_User     FOREIGN KEY (UserId)   REFERENCES AspNetUsers(Id)        ON DELETE SET NULL,
    CONSTRAINT FK_Resources_Category FOREIGN KEY (Category) REFERENCES ResourceCategories(Code) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ===== Behov =====
CREATE TABLE Needs (
    Id               INT           NOT NULL AUTO_INCREMENT,
    Name             VARCHAR(200)  NOT NULL,
    Description      VARCHAR(4000) NOT NULL,
    Category         VARCHAR(50)   NOT NULL,
    Priority         VARCHAR(50)   NOT NULL,
    Status           INT           NOT NULL DEFAULT 0,
    PhoneNumber      VARCHAR(20)   NOT NULL,
    CreatedAt        DATETIME(6)   NOT NULL,
    NeededBy         DATETIME(6)   NOT NULL,
    AreaGeoJson      LONGTEXT      NULL,
    AreaSquareMeters DOUBLE        NULL,
    PerimeterMeters  DOUBLE        NULL,
    Latitude         DOUBLE        NOT NULL,
    Longitude        DOUBLE        NOT NULL,
    UserId           VARCHAR(255)  NULL,
    CONSTRAINT PK_Needs PRIMARY KEY (Id),
    CONSTRAINT FK_Needs_User     FOREIGN KEY (UserId)   REFERENCES AspNetUsers(Id)      ON DELETE SET NULL,
    CONSTRAINT FK_Needs_Category FOREIGN KEY (Category) REFERENCES NeedCategories(Code) ON DELETE RESTRICT,
    CONSTRAINT FK_Needs_Priority FOREIGN KEY (Priority) REFERENCES Priorities(Code)     ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ===== Tildelinger =====
-- RESTRICT med vilje: en tildeling skal aldri forsvinne stille fordi noen sletter et behov
-- eller en ressurs. Applikasjonen må rydde eksplisitt. Se diskusjonen i besvarelsen.
CREATE TABLE Allocations (
    Id         INT         NOT NULL AUTO_INCREMENT,
    NeedId     INT         NOT NULL,
    ResourceId INT         NOT NULL,
    Quantity   INT         NOT NULL DEFAULT 1,
    CreatedAt  DATETIME(6) NOT NULL,
    CONSTRAINT PK_Allocations PRIMARY KEY (Id),
    CONSTRAINT FK_Allocations_Need     FOREIGN KEY (NeedId)     REFERENCES Needs(Id)     ON DELETE RESTRICT,
    CONSTRAINT FK_Allocations_Resource FOREIGN KEY (ResourceId) REFERENCES Resources(Id) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ===== Varsler =====
CREATE TABLE Notifications (
    Id        INT          NOT NULL AUTO_INCREMENT,
    UserId    VARCHAR(255) NOT NULL,
    Message   VARCHAR(500) NOT NULL,
    CreatedAt DATETIME(6)  NOT NULL,
    IsRead    TINYINT(1)   NOT NULL DEFAULT 0,
    CONSTRAINT PK_Notifications PRIMARY KEY (Id),
    CONSTRAINT FK_Notifications_User FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ===== Endringslogg =====
-- SET NULL, ikke CASCADE: loggen skal overleve at brukeren slettes.
CREATE TABLE AuditLogs (
    Id         INT          NOT NULL AUTO_INCREMENT,
    Timestamp  DATETIME(6)  NOT NULL,
    UserId     VARCHAR(255) NULL,
    Action     VARCHAR(30)  NOT NULL,
    EntityType VARCHAR(100) NOT NULL,
    EntityId   INT          NOT NULL,
    FieldName  VARCHAR(100) NULL,
    OldValue   LONGTEXT     NULL,
    NewValue   LONGTEXT     NULL,
    CONSTRAINT PK_AuditLogs PRIMARY KEY (Id),
    CONSTRAINT FK_AuditLogs_User FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
