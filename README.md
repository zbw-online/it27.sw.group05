# Auftragsverwaltung

Blazor-Webanwendung zur Verwaltung von Kunden, Artikeln, Artikelgruppen
und Aufträgen für eine Muster AG. Die Anwendung bildet den kompletten
Ablauf einer Bestellung ab: Kundenadressen mit Gültigkeitszeitraum,
kaskadierende Artikelkategorien, Lagerbestandsführung inklusive
Abgleichmechanismus sowie die Auftragserfassung selbst.

Technologien: **.NET 9 SDK / C#**, **ASP.NET Core Blazor (Server, Interactive)**,
**Entity Framework Core (Code First, temporale Tabellen)**, **MS SQL Server**,
**MSTest**, **Reqnroll**, **bUnit**, **Playwright**, **Testcontainers**,
**Azure DevOps Pipelines**, **Docker**

------------------------------------------------------------------------

## Inhaltsverzeichnis

- [Architektur](#architektur)
- [Schnellstart](#schnellstart)
  - [Variante A: mit Docker](#variante-a-mit-docker-empfohlen)
  - [Variante B: mit vorhandenem SQL Server](#variante-b-mit-vorhandenem-sql-server)
- [Voraussetzungen](#voraussetzungen)
- [Repository klonen](#repository-klonen)
- [Dependencies wiederherstellen](#dependencies-wiederherstellen)
- [Verbindungszeichenfolge einrichten](#verbindungszeichenfolge-einrichten)
  - [User Secrets](#user-secrets)
  - [Umgebungsvariable](#umgebungsvariable)
  - [Beispiele für verschiedene SQL-Server-Varianten](#beispiele-für-verschiedene-sql-server-varianten)
- [Datenbank erstellen](#datenbank-erstellen)
- [Anwendung starten](#anwendung-starten)
- [HTTPS-Entwicklungszertifikat](#https-entwicklungszertifikat)
- [Tests ausführen](#tests-ausführen)
- [Docker / Testcontainers](#docker--testcontainers)
- [Lagerbestand-Abgleich](#lagerbestand-abgleich)
- [Kunden-Datenaustausch](#kunden-datenaustausch)
- [Produktion: Publish und Docker](#produktion-publish-und-docker)
- [Azure-Pipelines](#azure-pipelines)
- [Git-Workflow](#git-workflow)
- [Fehlerbehebung](#fehlerbehebung)

------------------------------------------------------------------------

## Architektur

Die Lösung folgt einer klassischen Schichtenarchitektur (Clean
Architecture):

- **SharedKernel** – gemeinsame Basistypen (`Result`, Value Objects,
  Domain Events), ohne Abhängigkeit auf andere Projekte.
- **OrderManagement.Domain** – Aggregate, Entitäten, Value Objects und
  Geschäftsregeln (`Customer`, `Order`, `Article`, …). Keine
  Abhängigkeit auf Infrastruktur oder UI.
- **OrderManagement.Application** – Use Cases (ein Anwendungsfall pro
  Ordner unter `Features/…`), Commands, Queries und DTOs. Orchestriert
  die Domäne, kennt aber keine konkrete Datenbank- oder UI-Technologie.
- **OrderManagement.Infrastructure** – EF-Core-`DbContext`,
  Entity-Konfigurationen, Migrationen und Repository-Implementierungen.
- **OrderManagement.Presentation.Blazor** – die ausführbare
  ASP.NET-Core-Anwendung (Blazor Server, Interactive Render Mode).

Tests liegen unter `tests/` und sind nach Testebene benannt
(`*.Tests`, `*.IntegrationTests`, `*.AcceptanceTests`,
`*.PlaywrightTests`) sowie in einem gemeinsamen Hilfsprojekt
`OrderManagement.TestSupport` für die Testcontainers-Infrastruktur.
Details siehe [Tests ausführen](#tests-ausführen).

------------------------------------------------------------------------

## Schnellstart

Beide Varianten benötigen zuerst [Repository klonen](#repository-klonen),
[Dependencies wiederherstellen](#dependencies-wiederherstellen) und ein
[HTTPS-Entwicklungszertifikat](#https-entwicklungszertifikat).

### Variante A: mit Docker (empfohlen)

Die einfachste Variante ohne lokale SQL-Server-Installation. Ein
temporärer SQL-Server-Container übernimmt die Datenbank:

``` ps
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Ihr_Passwort123!" `
  -p 1433:1433 --name sql-dev `
  -d mcr.microsoft.com/mssql/server:2022-CU26-ubuntu-22.04
```

Danach die Verbindungszeichenfolge wie unter
[SQL Server in Docker](#beispiele-für-verschiedene-sql-server-varianten)
beschrieben setzen, mit [Datenbank erstellen](#datenbank-erstellen)
migrieren und die Anwendung [starten](#anwendung-starten).

### Variante B: mit vorhandenem SQL Server

Falls bereits eine SQL-Server-Instanz vorhanden ist (LocalDB, SQL
Express, eine lokale Vollinstallation oder ein entfernter Server):
Verbindungszeichenfolge gemäss den passenden
[Beispielen](#beispiele-für-verschiedene-sql-server-varianten) setzen,
dann direkt mit [Datenbank erstellen](#datenbank-erstellen) fortfahren.

------------------------------------------------------------------------

## Voraussetzungen

Installiert sein müssen:

- **.NET 10 SDK**, exakte Version gemäss `global.json`
  (`10.0.100`, `rollForward: latestFeature` – jede neuere 10.0.x-Version
  genügt). Alle Projekte (Anwendung und Tests) zielen einheitlich auf
  `net10.0`.
- **Git**
- Für Variante A oder für die Docker-abhängigen Testebenen:
  **Docker Desktop** (bzw. eine Docker-Engine, unter der Linux-Container
  laufen)
- Für Variante B ohne Docker: eine erreichbare **SQL-Server-Instanz**
  (LocalDB, SQL Express, Vollinstallation oder entfernter Server)

Optional:

- SQL Server Management Studio (SSMS) oder Azure Data Studio
- `dotnet-ef`-CLI (wird für Migrationsbefehle benötigt):

``` ps
dotnet tool install --global dotnet-ef
```

------------------------------------------------------------------------

## Repository klonen

``` ps
git clone https://github.com/zbw-online/it27.sw.group05.git
cd it27.sw.group05
git checkout Develop
```

`Develop` ist der Integrationsbranch, von dem aus neue Feature-Branches
erstellt werden (siehe [Git-Workflow](#git-workflow)).

------------------------------------------------------------------------

## Dependencies wiederherstellen

``` ps
dotnet restore
```

------------------------------------------------------------------------

## Verbindungszeichenfolge einrichten

Die Anwendung liest die Verbindungszeichenfolge aus der
Konfiguration unter dem Schlüssel `ConnectionStrings:OrderManagement`.
Der Standardwert in `appsettings.json`
(`Server=.;Database=OrderManagement;Trusted_Connection=True;TrustServerCertificate=True;`)
ist nur ein Platzhalter für eine lokale Standardinstallation und sollte
für die eigene Umgebung überschrieben werden – über User Secrets (lokale
Entwicklung) oder eine Umgebungsvariable (Server/Container).

**Enthält niemals echte Zugangsdaten im Repository.**

### User Secrets

Startup-Projekt ist die Blazor-Anwendung; sie und
`OrderManagement.Infrastructure` teilen sich dieselbe `UserSecretsId`,
damit sowohl die Laufzeit-App als auch das EF-Core-Tooling dieselbe
Verbindungszeichenfolge lesen.

``` ps
dotnet user-secrets init --project src\OrderManagement.Presentation.Blazor\OrderManagement.Presentation.Blazor.csproj

dotnet user-secrets set "ConnectionStrings:OrderManagement" "Server=.;Database=OrderManagement;Trusted_Connection=True;TrustServerCertificate=True;" --project src\OrderManagement.Presentation.Blazor\OrderManagement.Presentation.Blazor.csproj
```

### Umgebungsvariable

Für Server- oder Container-Betrieb (z. B. das produktive Docker-Image,
siehe [Produktion](#produktion-publish-und-docker)) wird dieselbe
Konfiguration stattdessen über eine Umgebungsvariable gesetzt
(`:` wird dabei zu `__`):

``` ps
$env:ConnectionStrings__OrderManagement = "Server=.;Database=OrderManagement;Trusted_Connection=True;TrustServerCertificate=True;"
```

### Beispiele für verschiedene SQL-Server-Varianten

Alle Beispiele sind Platzhalter – Server, Datenbankname und
Zugangsdaten der eigenen Umgebung anpassen. Nirgends echte Passwörter
einsetzen.

**LocalDB**

``` text
Server=(localdb)\MSSQLLocalDB;Database=OrderManagement;Trusted_Connection=True;TrustServerCertificate=True;
```

**SQL Express**

``` text
Server=localhost\SQLEXPRESS;Database=OrderManagement;Trusted_Connection=True;TrustServerCertificate=True;
```

**Windows-Authentifizierung (Vollinstallation)**

``` text
Server=.;Database=OrderManagement;Trusted_Connection=True;TrustServerCertificate=True;
```

**SQL-Server-Authentifizierung**

``` text
Server=.;Database=OrderManagement;User Id=<Benutzername>;Password=<Ihr_Passwort>;TrustServerCertificate=True;
```

**SQL Server in Docker** (passend zum Container aus
[Schnellstart Variante A](#variante-a-mit-docker-empfohlen))

``` text
Server=localhost,1433;Database=OrderManagement;User Id=sa;Password=<Ihr_Passwort>;TrustServerCertificate=True;
```

**Entfernter SQL Server**

``` text
Server=<hostname-oder-ip>,1433;Database=OrderManagement;User Id=<Benutzername>;Password=<Ihr_Passwort>;TrustServerCertificate=True;Encrypt=True;
```

------------------------------------------------------------------------

## Datenbank erstellen

Migrationen anwenden (erstellt die Datenbank, falls sie noch nicht
existiert):

``` ps
dotnet ef database update --project src\OrderManagement.Infrastructure\OrderManagement.Infrastructure.csproj --startup-project src\OrderManagement.Presentation.Blazor\OrderManagement.Presentation.Blazor.csproj
```

Neue Migration erstellen (nach Änderungen am Domänenmodell):

``` ps
dotnet ef migrations add <MigrationName> --project src\OrderManagement.Infrastructure\OrderManagement.Infrastructure.csproj --startup-project src\OrderManagement.Presentation.Blazor\OrderManagement.Presentation.Blazor.csproj
```

------------------------------------------------------------------------

## Anwendung starten

``` ps
dotnet run --project src\OrderManagement.Presentation.Blazor\OrderManagement.Presentation.Blazor.csproj
```

Die Anwendung ist danach unter folgender Adresse erreichbar:

- HTTP: `http://localhost:5037`
- HTTPS: `https://localhost:7103` (nur mit vertrautem
  Entwicklungszertifikat, siehe nächster Abschnitt)

------------------------------------------------------------------------

## HTTPS-Entwicklungszertifikat

Für den HTTPS-Start muss das lokale .NET-Entwicklungszertifikat
vertraut sein. Bei Zertifikatsproblemen zuerst bereinigen, dann neu
vertrauen:

``` ps
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

------------------------------------------------------------------------

## Tests ausführen

Alle Tests ausführen:

``` ps
dotnet test
```

Nach Testebene:

``` ps
# Domain-Unit-Tests (Invarianten)
dotnet test .\tests\OrderManagement.Domain.Tests\OrderManagement.Domain.Tests.csproj

# Application-Unit-Tests (Orchestrierung der Use Cases)
dotnet test .\tests\OrderManagement.Application.Tests\OrderManagement.Application.Tests.csproj

# SharedKernel-Unit-Tests
dotnet test .\tests\SharedKernel.Tests\SharedKernel.Tests.csproj

# bUnit-Komponententests (Blazor-UI-Zustand und Barrierefreiheit)
dotnet test .\tests\OrderManagement.Presentation.Blazor.Tests\OrderManagement.Presentation.Blazor.Tests.csproj

# Infrastructure-Integrationstests (echter SQL Server via Testcontainers)
dotnet test .\tests\OrderManagement.Infrastructure.IntegrationTests\OrderManagement.Infrastructure.IntegrationTests.csproj

# Reqnroll-Akzeptanztests (Geschäftsverhalten, echter SQL Server via Testcontainers)
dotnet test .\tests\OrderManagement.AcceptanceTests\OrderManagement.AcceptanceTests.csproj

# Playwright-End-to-End-Tests (kritische Full-Stack-UI-Abläufe)
dotnet test .\tests\OrderManagement.PlaywrightTests\OrderManagement.PlaywrightTests.csproj
```

`OrderManagement.Infrastructure.IntegrationTests`,
`OrderManagement.AcceptanceTests` und `OrderManagement.PlaywrightTests`
benötigen einen laufenden **Docker Desktop** (siehe nächster
Abschnitt).

Für `OrderManagement.PlaywrightTests` muss zusätzlich einmalig der
Playwright-Browser installiert werden:

``` ps
dotnet build .\tests\OrderManagement.PlaywrightTests\OrderManagement.PlaywrightTests.csproj
pwsh .\tests\OrderManagement.PlaywrightTests\bin\Debug\net10.0\playwright.ps1 install chromium
```

Testabdeckung erfassen:

``` ps
dotnet test --settings coverage.runsettings --collect:"XPlat Code Coverage"
```

------------------------------------------------------------------------

## Docker / Testcontainers

`OrderManagement.Infrastructure.IntegrationTests`,
`OrderManagement.AcceptanceTests` und `OrderManagement.PlaywrightTests`
starten für jeden Testlauf automatisch einen eigenen, isolierten
SQL-Server-Container (`mcr.microsoft.com/mssql/server:2022-CU26-ubuntu-22.04`,
über die gemeinsame Testinfrastruktur `OrderManagement.TestSupport`).
Es muss keine eigene Testdatenbank angelegt oder gepflegt werden –
Testcontainers übernimmt Start, Bereitschaftsprüfung und Aufräumen
automatisch. Innerhalb eines Testlaufs erhält jeder Test bzw. jedes
Szenario eine eigene, eindeutig benannte Datenbank auf demselben
Container.

Voraussetzung ist ausschliesslich, dass Docker läuft und erreichbar
ist. Läuft Docker nicht, brechen diese Testprojekte beim Start mit
einer klaren Fehlermeldung ab (siehe
[Fehlerbehebung](#fehlerbehebung)) – das ist kein Programmfehler.

------------------------------------------------------------------------

## Lagerbestand-Abgleich

Jeder Auftrag trägt ein Flag `IsInventoryApplied`, das festhält, ob
sein Lagerbestandseffekt bereits verbucht wurde. Neue Aufträge werden
immer mit `IsInventoryApplied = true` angelegt (der Bestand wird in
derselben Transaktion wie der Auftrag reduziert). Aufträge, die vor
dieser Version bereits in der Datenbank existierten, gelten als nicht
abgeglichen, da sich aus dem reinen Lagerbestand nicht ableiten lässt,
ob ihr Bestand bereits berücksichtigt wurde.

**Wichtig:** Der Abgleich läuft nie automatisch beim Start der
Anwendung und nie automatisch gegen eine bestehende Datenbank. Er muss
über die Kommandozeile explizit gestartet werden.

Testlauf (zeigt nur einen Bericht an, ändert nichts):

``` ps
dotnet run --project src\OrderManagement.Presentation.Blazor\OrderManagement.Presentation.Blazor.csproj -- reconcile-inventory
```

Der Bericht enthält die betroffenen Auftragsnummern, die betroffenen
Artikel mit aktuellem Bestand, abzuziehender Menge und resultierendem
Bestand sowie allfällige Konflikte (z. B. unzureichender
Lagerbestand).

Abgleich anwenden (schreibt die Änderungen in einer einzigen
Transaktion):

``` ps
dotnet run --project src\OrderManagement.Presentation.Blazor\OrderManagement.Presentation.Blazor.csproj -- reconcile-inventory --apply
```

Verhalten:

- Aufträge, die bereits abgeglichen sind (`IsInventoryApplied = true`),
  werden ignoriert. Ein wiederholter Aufruf ist ein No-Op.
- Wird für **irgendeinen** betroffenen Artikel ein unzureichender
  Lagerbestand festgestellt, wird der gesamte Abgleich abgelehnt und
  **keine** Änderung vorgenommen (kein Teilabgleich).
- Da im aktuellen Domänenmodell kein `Order.Status`
  (Entwurf/storniert/…) existiert, werden alle nicht abgeglichenen
  Aufträge als lagerrelevant behandelt. Sollte künftig ein
  Auftragsstatus eingeführt werden, muss diese Annahme überprüft
  werden.

------------------------------------------------------------------------

## Kunden-Datenaustausch

Die Kundenverwaltung (`/kunden`) erlaubt den Austausch von
Kundenstammdaten mit anderen Systemen über den Import und Export von
JSON- oder XML-Dateien. Damit lassen sich Kundendaten in Serien
erfassen oder für einen bestimmten historischen Zeitpunkt (Stichtag)
exportieren, ohne jeden Kunden einzeln zu erfassen.

### Unterstützte Formate

JSON und XML werden über das Strategy-Pattern
(`ICustomerDataSerializer` / `ICustomerDataSerializerResolver` in
`OrderManagement.Application`) angeboten. Die JSON-Implementierung
verwendet `System.Text.Json`, die XML-Implementierung `XmlSerializer`
(`OrderManagement.Infrastructure/Serialization/Customers`) – beide ohne
Drittanbieter-Pakete.

JSON-Beispiel:

```json
[
  {
    "customerNumber": "CU00001",
    "lastName": "Muster",
    "surName": "Hans",
    "email": "hans.muster@example.ch",
    "website": "www.example.ch",
    "address": {
      "validFrom": "2026-01-01",
      "street": "Musterstrasse",
      "houseNumber": "10",
      "postalCode": "8000",
      "city": "Zürich",
      "countryCode": "CH"
    }
  }
]
```

XML-Beispiel:

```xml
<Kunden>
  <Kunde CustomerNumber="CU00001">
    <LastName>Muster</LastName>
    <SurName>Hans</SurName>
    <Email>hans.muster@example.ch</Email>
    <Website>www.example.ch</Website>
    <Address>
      <ValidFrom>2026-01-01</ValidFrom>
      <Street>Musterstrasse</Street>
      <HouseNumber>10</HouseNumber>
      <PostalCode>8000</PostalCode>
      <City>Zürich</City>
      <CountryCode>CH</CountryCode>
    </Address>
  </Kunde>
</Kunden>
```

Beide Formate transportieren dieselben semantischen Informationen.
JSON wird deterministisch UTF-8, camelCase und nach Kundennummer
sortiert exportiert; XML analog mit `Kunde`-Elementen und
`CustomerNumber` als Attribut. Beim Import werden unbekannte
Eigenschaften/Elemente, doppelte JSON-Eigenschaften, DTD/XXE-Inhalte
und überhöhte Verschachtelungstiefe abgelehnt.

### Zeitreise-Export (Stichtag)

Der Export akzeptiert einen **Stichtag** (Datum und Uhrzeit) und
liefert den Datenstand aller Kunden **zu genau diesem Zeitpunkt**:

- Der im Dialog eingegebene Stichtag wird als **Schweizer Ortszeit**
  (`Europe/Zurich`) interpretiert und für die SQL-Server-Zeitreise-
  Abfrage (`FOR SYSTEM_TIME` / EF Core `TemporalAsOf`) in UTC
  umgerechnet.
- Zusätzlich wird die **Geschäftsgültigkeit der Adresse**
  (`ValidFrom`/`ValidTo` auf `CustomerAddress`) anhand des
  Schweizer Kalenderdatums des Stichtags ausgewertet – unabhängig
  von der SQL-Systemzeit.
- Ein Kunde, der **nach** dem Stichtag gelöscht wurde, erscheint
  weiterhin im Export, sofern er zum Stichtag existierte (die
  History-Tabelle der temporalen Kundentabelle macht dies möglich).
- Ein Kunde ohne zum Stichtag gültige Adresse wird mit `address: null`
  exportiert – das ist kein Fehler.
- Namens-, E-Mail- und Website-Änderungen **nach** dem Stichtag werden
  nicht berücksichtigt; exportiert wird der zum Stichtag gültige Stand.

Exportierte Dateien werden **nicht** dauerhaft auf dem Server
gespeichert, sondern direkt an den Browser zum Download übergeben
(Dateiname z. B. `kundendaten-20260905-1830.json`).

### Import: Create-only und atomar

Der Import erstellt **ausschliesslich neue Kunden** – bestehende
Kunden werden nie aktualisiert oder überschrieben. Der komplette
Import ist **atomar**:

1. Die gesamte Datei wird zuerst vollständig validiert (Format,
   Grösse, Anzahl Datensätze, Pflichtfelder, Kundennummer- und
   E-Mail-Format gemäss den Domain-Regeln in `Customer`).
2. Duplikate werden geprüft – sowohl **innerhalb der Datei**
   (Kundennummer und E-Mail, E-Mail ohne Gross-/Kleinschreibung) als
   auch **gegen die bestehende Datenbank**.
3. Enthält die Datei auch nur einen ungültigen oder bereits
   existierenden Datensatz, wird **kein einziger** Kunde importiert.
4. Erst wenn die gesamte Datei gültig ist, werden alle neuen Kunden
   in einer einzigen Transaktion (`CommitAsync`) gespeichert.

Die Vorschau (`Datei prüfen`) und der eigentliche Import
(`In Datenbank importieren`) verwenden denselben
`CustomerImportPlanBuilder`, damit keine Validierungslogik doppelt
gepflegt werden muss; der Import validiert beim Ausführen zusätzlich
erneut, falls sich der Datenbestand seit der Vorschau geändert hat.

### Konfigurierbare Limiten

Die Sicherheits- und Grössenlimiten für Import/Export sind über den
Abschnitt `CustomerDataExchange` in `appsettings.json` konfigurierbar
(gebunden via `IOptions<CustomerDataExchangeOptions>`):

```json
"CustomerDataExchange": {
  "MaxFileSizeBytes": 2097152,
  "MaxCustomerCount": 5000,
  "MaxJsonDepth": 64,
  "MaxXmlCharacters": 20000000
}
```

| Einstellung        | Standardwert | Bedeutung                                   |
| ------------------- | ------------: | -------------------------------------------- |
| `MaxFileSizeBytes`  |    2'097'152 | Maximale Dateigrösse in Bytes (2 MB)         |
| `MaxCustomerCount`  |         5000 | Maximale Anzahl Kundendatensätze pro Datei   |
| `MaxJsonDepth`      |           64 | Maximale JSON-Verschachtelungstiefe          |
| `MaxXmlCharacters`  |   20'000'000 | Maximale Zeichenanzahl im XML-Dokument       |

### Ein weiteres Format ergänzen (z. B. CSV)

Dank Strategy-Pattern erfordert ein zusätzliches Format **keine
Änderung** an den bestehenden JSON-/XML-Implementierungen oder am
Import-/Export-Workflow:

1. Neue Klasse `CsvCustomerDataSerializer` in
   `OrderManagement.Infrastructure/Serialization/Customers` erstellen,
   die `ICustomerDataSerializer` implementiert (`Format`,
   `FileExtension`, `MediaType`, `SerializeAsync`, `DeserializeAsync`).
2. Registrierung als weitere Implementierung in
   `OrderManagement.Infrastructure.InfrastructureServiceCollectionExtensions`:
   `services.AddSingleton<ICustomerDataSerializer, CsvCustomerDataSerializer>();`
3. `CustomerDataFormat` um `Csv` erweitern.

Der `CustomerDataSerializerResolver` sowie alle Use Cases
(`ImportCustomerData`, `ValidateCustomerDataImport`,
`ExportCustomerData`) benötigen keine Anpassung.

### Anmeldedaten sind bewusst ausgeschlossen

Passwörter und Authentifizierung gehören **nicht** zum Bounded
Context "Kunde" und sind daher weder in der Domain (`Customer`), noch
in den Datenaustausch-DTOs, der Benutzeroberfläche oder den
JSON-/XML-Formaten enthalten. Das ist eine bewusste
Architekturentscheidung – Anmeldedaten gehören in einen separaten
Authentifizierungs-Kontext – und **kein** fehlender Datenpunkt.

### Relevante Testbefehle

Die neuen Tests wurden in die bestehenden Testprojekte integriert
(kein neues Testprojekt). Gezielt ausführen:

``` ps
# Application-Unit-Tests (Import-/Export-Use-Cases, Validierungsplan)
dotnet test .\tests\OrderManagement.Application.Tests\OrderManagement.Application.Tests.csproj --filter "FullyQualifiedName~DataExchange"

# Infrastructure-Integrationstests (JSON/XML-Serialisierung, Zeitreise-Export, atomarer Import – echter SQL Server)
dotnet test .\tests\OrderManagement.Infrastructure.IntegrationTests\OrderManagement.Infrastructure.IntegrationTests.csproj --filter "FullyQualifiedName~Serialization|FullyQualifiedName~Temporal|FullyQualifiedName~ImportCustomerData"

# Reqnroll-Akzeptanztests (Geschäftsverhalten End-to-End – echter SQL Server)
dotnet test .\tests\OrderManagement.AcceptanceTests\OrderManagement.AcceptanceTests.csproj --filter "FullyQualifiedName~CustomerDataExchange"

# bUnit-Komponententests (Import-/Export-Dialoge)
dotnet test .\tests\OrderManagement.Presentation.Blazor.Tests\OrderManagement.Presentation.Blazor.Tests.csproj --filter "FullyQualifiedName~CustomersTests"

# Playwright-End-to-End-Tests (Download, Upload, Tastaturbedienung, Responsive-Verhalten)
dotnet test .\tests\OrderManagement.PlaywrightTests\OrderManagement.PlaywrightTests.csproj --filter "FullyQualifiedName~CustomerDataExchangeTests"
```

------------------------------------------------------------------------

## Produktion: Publish und Docker

Produktions-Build erstellen:

``` ps
dotnet publish src\OrderManagement.Presentation.Blazor\OrderManagement.Presentation.Blazor.csproj --configuration Release --output .\publish
```

Produktions-Docker-Image bauen und lokal ausführen:

``` ps
docker build -t ordermanagement-blazor -f Dockerfile .

docker run -p 8080:8080 `
  -e ConnectionStrings__OrderManagement="Server=<host>,1433;Database=OrderManagement;User Id=<Benutzername>;Password=<Ihr_Passwort>;TrustServerCertificate=True;" `
  ordermanagement-blazor
```

Das Image führt beim Start **keine** Datenbankmigration aus. Die
Datenbank muss vorher über `dotnet ef database update` oder das
EF-Core-Migrationsbundle (von der Azure-Release-Pipeline erzeugt,
siehe unten) auf den aktuellen Stand gebracht werden.

------------------------------------------------------------------------

## Azure-Pipelines

Unter `.azuredevops/` liegen drei Pipelines:

- **`azure-pipelines-format.yml`** – schnelle Formatierungsprüfung
  (`dotnet format --verify-no-changes`) für Pull Requests und Pushes.
- **`azure-pipelines-test.yml`** – mehrstufige Test-Pipeline:
  Build, Formatierungsprüfung, Unit-/bUnit-Tests, SQL-Server-
  Integrationstests, Reqnroll-Akzeptanztests, Playwright-Tests,
  anschliessend Testabdeckungsbericht mit Mindestschwelle von 50 %
  Zeilenabdeckung. Läuft für Pull Requests nach `Develop` und `main`
  sowie für Pushes auf `feature/*`, `Develop` und `main`. Für Pull
  Requests nach `main` wird zusätzlich geprüft, dass der Quellbranch
  `Develop` ist (Ausnahme nur über die Pipeline-Variable
  `AllowNonDevelopSourceForMain` für einen autorisierten
  Release-/Hotfix-Vorgang). Artefakte: TRX-Testergebnisse,
  Testabdeckungsbericht, bei Playwright-Fehlschlägen zusätzlich
  Traces/Screenshots.
- **`azure-pipelines-release.yml`** – läuft bei Push auf `main` sowie
  bei `Abgabe_*`-Tags: Build, Tests, Konvertierung von `docs/*.docx`
  nach PDF, `dotnet publish`, EF-Core-Migrationsbundle, Bau und
  Smoke-Test des produktiven Docker-Images. Ein GitHub Release mit
  PDF-Anhang wird ausschliesslich bei `Abgabe_*`-Tags erstellt, damit
  ein gewöhnlicher Merge nach `main` kein zusätzliches, unkontrolliertes
  Release erzeugt. Ein Docker-Push in eine Registry erfolgt nur, wenn
  die Pipeline-Variable `dockerRegistryServiceConnection` auf eine
  echte Service-Connection gesetzt ist.

Empfohlener GitHub-Branchschutz für `Develop` und `main` (kann von den
YAML-Pipelines allein nicht erzwungen werden):

- Pull Request vor dem Merge erforderlich
- Erfolgreiche Azure-Pipelines-Prüfungen erforderlich
- Force-Push verhindern
- Löschen des Branches verhindern
- Umgehung der Regeln auf berechtigte Personen einschränken

------------------------------------------------------------------------

## Git-Workflow

Vorgesehener Ablauf: `feature/* → Develop → main`.

Feature-Branch von `Develop` erstellen:

``` ps
git checkout Develop
git pull
git checkout -b feature/Beispiel-Funktion
```

Änderungen committen:

``` ps
git status
git add .
git commit -m "Implement core logic"
git push origin feature/Beispiel-Funktion
```

Empfohlener Ablauf:

1. Feature-Branch von `Develop` erstellen
2. Änderungen implementieren
3. Tests ausführen
4. Code formatieren (`dotnet format`)
5. Commit + Push
6. Pull Request nach `Develop` erstellen
7. Nach Freigabe: Pull Request von `Develop` nach `main`

------------------------------------------------------------------------

## Fehlerbehebung

**Docker läuft nicht**
`OrderManagement.Infrastructure.IntegrationTests`,
`OrderManagement.AcceptanceTests` und `OrderManagement.PlaywrightTests`
brechen beim Start mit einer Meldung ab, dass Docker nicht erreichbar
ist. Docker Desktop starten und den Testlauf wiederholen.

**Verbindung zum SQL Server schlägt fehl**
Verbindungszeichenfolge prüfen (Servername, Authentifizierung,
`TrustServerCertificate=True` bei selbstsigniertem Zertifikat), sowie
ob die SQL-Server-Instanz erreichbar ist und der TCP/IP-Zugriff
aktiviert ist (bei SQL Express standardmässig deaktiviert).

**Nicht vertrautes HTTPS-Zertifikat im Browser**
Siehe [HTTPS-Entwicklungszertifikat](#https-entwicklungszertifikat):
`dotnet dev-certs https --clean` gefolgt von
`dotnet dev-certs https --trust`.

**Migrationen wurden nicht angewendet**
`dotnet ef database update` gemäss
[Datenbank erstellen](#datenbank-erstellen) ausführen. Fehlermeldungen
zu fehlenden Tabellen oder Spalten deuten meist darauf hin, dass dieser
Schritt nach dem letzten Pull von `Develop` übersprungen wurde.

**Port bereits belegt**
`http://localhost:5037` bzw. `https://localhost:7103` werden von einem
anderen Prozess verwendet. Entweder den blockierenden Prozess beenden
oder in `Properties/launchSettings.json` einen anderen Port eintragen.

**Testcontainers-Start dauert lange oder schlägt beim ersten Mal fehl**
Der erste Start lädt das SQL-Server-Container-Image herunter, was je
nach Internetverbindung einige Minuten dauern kann. Nachfolgende
Läufe sind deutlich schneller, da das Image lokal zwischengespeichert
ist.
