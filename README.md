# Auftragsverwaltung

Blazor-Webanwendung zur Verwaltung von Kunden, Artikeln, Artikelgruppen
und Aufträgen.
Technologien: **.NET / C#**, **Blazor (Server, Interactive)**,
**Entity Framework Core (Code First)**, **MS SQL Server**

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

Für lokale Entwicklung werden **User Secrets** verwendet. Startup-Projekt
ist die Blazor-Anwendung; sie und `OrderManagement.Infrastructure` teilen
sich dieselbe `UserSecretsId`, damit sowohl die Laufzeit-App als auch das
EF-Core-Tooling dieselbe Connection String lesen.

User-Secrets initialisieren:

``` ps
dotnet user-secrets init --project src\OrderManagement.Presentation.Blazor\OrderManagement.Presentation.Blazor.csproj
```

Connection String setzen:

``` ps
dotnet user-secrets set "ConnectionStrings:OrderManagement" "Server=.;Database=OrderManagement;Trusted_Connection=true;TrustServerCertificate=true;" --project src\OrderManagement.Presentation.Blazor\OrderManagement.Presentation.Blazor.csproj
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
dotnet ef database update --project src\OrderManagement.Infrastructure\OrderManagement.Infrastructure.csproj --startup-project src\OrderManagement.Presentation.Blazor\OrderManagement.Presentation.Blazor.csproj
```

Neue Migration erstellen:

``` ps
dotnet ef migrations add InitialCreate --project src\OrderManagement.Infrastructure\OrderManagement.Infrastructure.csproj --startup-project src\OrderManagement.Presentation.Blazor\OrderManagement.Presentation.Blazor.csproj
```

------------------------------------------------------------------------

# Tests ausführen

Alle Tests ausführen:

``` ps
dotnet test
```

Bestimmtes Testprojekt ausführen:

``` ps
dotnet test .\tests\OrderManagement.Domain.Tests\OrderManagement.Domain.Tests.csproj
dotnet test .\tests\SharedKernel.Tests\SharedKernel.Tests.csproj
dotnet test .\tests\OrderManagement.Application.Tests\OrderManagement.Application.Tests.csproj
dotnet test .\tests\OrderManagement.Infrastructure.IntegrationTests\OrderManagement.Infrastructure.IntegrationTests.csproj
dotnet test .\tests\OrderManagement.AcceptanceTests\OrderManagement.AcceptanceTests.csproj
```

`OrderManagement.Infrastructure.IntegrationTests` und
`OrderManagement.AcceptanceTests` benötigen einen laufenden
**Docker Desktop** (SQL Server Testcontainers).

Testabdeckung erfassen:

``` ps
dotnet test --settings coverage.runsettings --collect:"XPlat Code Coverage"
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
