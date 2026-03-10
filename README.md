# Auftragsverwaltung

Desktop-Anwendung zur Verwaltung von Kunden, Artikeln, Artikelgruppen
und Aufträgen.
Technologien: **.NET / C#**, **Entity Framework Core (Code First)**,
**MS SQL Server**

------------------------------------------------------------------------

# Inhaltsverzeichnis

-   [Voraussetzungen](#voraussetzungen)
-   [Dependencies wiederherstellen](#dependencies-wiederherstellen)
-   [EF Core Verbindung einrichten](#ef-core-verbindung-einrichten)
-   [Datenbank erstellen](#datenbank-erstellen)
-   [Tests ausführen](#tests-ausführen)
-   [Code formatieren](#code-formatieren)
-   [Git Workflow](#git-workflow)

------------------------------------------------------------------------

# Voraussetzungen

Installiert sein müssen:

-   Visual Studio 20XX
-   .NET SDK
-   MS SQL Server 
-   Git
-   Docker Desktop 

Optional:

-   SQL Server Management Studio (SSMS)
-   EF Core CLI

EF CLI installieren:

``` ps
dotnet tool install --global dotnet-ef
```

------------------------------------------------------------------------

# Dependencies wiederherstellen

``` ps
dotnet restore
```

------------------------------------------------------------------------

# EF Core Verbindung einrichten

Für lokale Entwicklung werden **User Secrets** verwendet.

In den **src Ordner wechseln**

``` ps
cd .\src
```

User-Secrets initialisieren (Pfad ggf. anpassen):

``` ps
dotnet user-secrets init --project "C:\Path\To\OrderManagement.Infrastructure\OrderManagement.Infrastructure.csproj"
```

Connection String setzen:

``` ps
dotnet user-secrets set "ConnectionStrings:OrderManagement" "Server=.;Database=OrderManagement;Trusted_Connection=true;TrustServerCertificate=true;" --project "C:\Path\To\OrderManagement.Infrastructure\OrderManagement.Infrastructure.csproj"
```

Alternative Beispiele:

LocalDB

``` ps
Server=(localdb)\MSSQLLocalDB;Database=OrderManagement;Trusted_Connection=true;
```

SQL Express

``` ps
Server=localhost\SQLEXPRESS;Database=OrderManagement;Trusted_Connection=true;
```

------------------------------------------------------------------------

# Datenbank erstellen

Migrationen anwenden:

``` ps
dotnet ef database update --project "C:\Path\To\OrderManagement.Infrastructure\OrderManagement.Infrastructure.csproj"
```

Neue Migration erstellen:

``` ps
dotnet ef migrations add InitialCreate --project "C:\Path\To\OrderManagement.Infrastructure\OrderManagement.Infrastructure.csproj"
```

------------------------------------------------------------------------

# Tests ausführen

Alle Tests ausführen:

``` ps
dotnet test
```

Bestimmtes Testprojekt ausführen:

``` ps
dotnet test .\tests\OrderManagement.Tests\OrderManagement.Tests.csproj
```

------------------------------------------------------------------------

# Code formatieren

Vor jedem Push ausführen:

``` ps
dotnet format
```

------------------------------------------------------------------------

# Git Workflow

Feature Branch wechseln:

``` ps
git checkout Feature_Beispiel_Branch
```

Änderungen committen:

``` ps
git status
git add .
git commit -m "Implement core logic"
git push origin Feature_Beispiel_Branch
```

Empfohlener Ablauf:

1.  Feature Branch erstellen
2.  Änderungen implementieren
3.  Tests ausführen
4.  Code formatieren
5.  Commit + Push
6.  Pull Request erstellen
