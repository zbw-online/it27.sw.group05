# Auftragsverwaltung


## Development

### Local commands (run before pushing)

```ps

dotnet format

```

### Push to Feature Branch

```ps

git checkout Feature_Beispiel_Branch
git status
git add .
git commit -m "Implement core logic"
git push origin Feature_Beispiel_Branch


```

### Setup user-secrets

#### In the Developer Command Prompt go into src folder with cd
#### •	Initialize:

```ps

dotnet user-secrets init --project 'C\Here\Must\Be\Your\Path\To\OrderManagement.Infrastructure.csproj'

```

####•	Set secret:
####•	    replace . in Server=. with your Server

```ps

dotnet user-secrets set "ConnectionStrings:OrderManagement" "Server=.;Database=OrderManagement;Trusted_Connection=true;TrustServerCertificate=true;" --project 'C\Here\Must\Be\Your\Path\To\OrderManagement.Infrastructure.csproj'

```
