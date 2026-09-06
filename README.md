# Auftragsverwaltung

Blazor-Webanwendung zur Verwaltung von Kunden, Artikeln, Artikelgruppen
und Aufträgen für eine Muster AG.

Technologien: **.NET 10 SDK / C#**, **ASP.NET Core Blazor (Server, Interactive)**,
**Entity Framework Core (Code First, temporale Tabellen)**, **MS SQL Server**,
**MSTest**, **Reqnroll**, **bUnit**, **Playwright**, **Testcontainers**,
**Azure DevOps Pipelines**, **Docker Compose**

## Voraussetzungen

- [Git](https://git-scm.com/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (inkl. Docker Compose)

Ein lokal installiertes .NET SDK oder ein lokaler SQL Server werden für den
Docker-Weg **nicht** benötigt.

## Starten

```powershell
git clone https://github.com/zbw-online/it27.sw.group05.git
cd it27.sw.group05
git checkout Develop
Copy-Item .env.example .env
docker compose up --build -d
```

Die `.env`-Datei enthält das lokale SQL-Server-Passwort und den Schalter für
Demo-Daten. Beim ersten Start wird die Datenbank automatisch migriert und
(sofern `SEED_DEMO_DATA=true`) mit Demo-Daten befüllt.

Anwendung: <http://localhost:8080>

Health-Check: <http://localhost:8080/health/live>

## Stoppen und zurücksetzen

```powershell
docker compose down
```

Die Datenbank bleibt dabei erhalten (persistentes Volume).

Für einen vollständigen Reset (**löscht das SQL-Server-Volume und damit alle
Daten**):

```powershell
docker compose down -v
docker compose up --build -d
```

## Tests ausführen

```powershell
docker compose -f compose.test.yaml up --build --abort-on-container-exit --exit-code-from tests
docker compose -f compose.test.yaml down -v
```

Dies baut einen isolierten Test-Stack (eigener SQL Server, eigene
Anwendungsinstanz, Test-Runner-Image mit Chromium) und führt sämtliche
automatisierten Tests aus - Architektur-, SharedKernel-, Domain-,
Application-, bUnit-, Infrastruktur-Integrations-, Reqnroll-Akzeptanz- und
Playwright-E2E-Tests. Ein fehlgeschlagener Test lässt den Befehl mit einem
Fehlercode fehlschlagen. Benötigt wird dafür nur Git und Docker Desktop.

Für den klassischen Weg (lokales .NET SDK, Testcontainers für isolierte
SQL-Server-Instanzen) funktioniert weiterhin:

```powershell
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release --no-build
```

## Deployment

Der Compose-Stack eignet sich als Ausgangspunkt für eine Linux-VM oder eine
Docker-Compose-fähige Hosting-Plattform. Er ist bewusst einfach gehalten und
**nicht production-hardened** - folgende Punkte bleiben Aufgabe des
Betreibers:

- Lokales Demo-Passwort in `.env` durch ein echtes, sicher verwaltetes
  Passwort ersetzen
- `SEED_DEMO_DATA=false` setzen
- Externes Secret-Management verwenden, sofern verfügbar (z. B. Azure Key
  Vault, Docker Secrets)
- Anwendung hinter TLS/Reverse-Proxy betreiben
- Persistenten Speicher und Backups für das SQL-Server-Volume einrichten
- Korrekt lizenzierte SQL-Server-Edition oder einen externen SQL
  Server/Azure SQL verwenden (die `Developer`-Edition ist nur für
  Entwicklung/Demo zulässig)
