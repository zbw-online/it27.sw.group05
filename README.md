# Auftragsverwaltung

Die Anwendung richtet sich an Sachbearbeiter und verwaltet Kunden, Adressen,
Artikel, Artikelgruppen und Aufträge. Anwendung und SQL Server laufen
containerisiert mit Docker Compose.

## Schnellstart

Voraussetzungen:

- [Git](https://git-scm.com/)
- Docker Desktop oder Docker Engine mit Docker Compose

Windows PowerShell:

```powershell
git clone https://github.com/zbw-online/it27.sw.group05.git
cd it27.sw.group05
Copy-Item .env.example .env
docker compose up --build -d
```

Linux oder macOS:

```bash
git clone https://github.com/zbw-online/it27.sw.group05.git
cd it27.sw.group05
cp .env.example .env
docker compose up --build -d
```

Vor dem Start muss in `.env` ein sicheres SQL-Server-Passwort gesetzt werden.

```text
Anwendung: http://localhost:8080
Health Check: http://localhost:8080/health/live
```

## Wichtige Befehle

Stack starten:

```bash
docker compose up --build -d
```

Status anzeigen:

```bash
docker compose ps
```

Logs anzeigen:

```bash
docker compose logs -f
```

Stack stoppen:

```bash
docker compose down
```

Datenbank und Volume vollständig löschen (**löscht alle lokalen Daten
dauerhaft**):

```bash
docker compose down -v
```

Tests ausführen:

```bash
docker compose -f compose.test.yaml up --build --abort-on-container-exit --exit-code-from tests
docker compose -f compose.test.yaml down -v
```

## Release erstellen

```bash
git fetch origin main
git tag -a Abgabe_Projekt origin/main -m "Release Abgabe_Projekt"
git push origin Abgabe_Projekt
```

- Zuerst `Develop` per Pull Request nach `main` mergen.
- Danach den Tag auf dem gewünschten `main`-Commit erstellen.
- Nur Tags mit dem Präfix `Abgabe_` lösen ein Release aus.
- Jeder Tag-Name muss eindeutig sein.
- Veröffentlichte Tags nicht verschieben oder wiederverwenden.
- GitHub stellt Source-Code-ZIP und TAR.GZ automatisch bereit.
