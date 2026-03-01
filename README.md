# Auftragsverwaltung
[![Board Status](https://dev.azure.com/zbw-it27-group05/39e176f9-8d80-4171-af1c-640eaaf80174/e95d0732-ca58-43d3-b205-fcf1f5e77ec9/_apis/work/boardbadge/9dcfd190-a035-4b2c-a2ed-fef0d36b75a7)](https://dev.azure.com/zbw-it27-group05/39e176f9-8d80-4171-af1c-640eaaf80174/_boards/board/t/e95d0732-ca58-43d3-b205-fcf1f5e77ec9/Stories/)

.......

[![Board Status](https://dev.azure.com/zbw-it27-group05/39e176f9-8d80-4171-af1c-640eaaf80174/e95d0732-ca58-43d3-b205-fcf1f5e77ec9/_apis/work/boardbadge/9dcfd190-a035-4b2c-a2ed-fef0d36b75a7)](https://dev.azure.com/zbw-it27-group05/39e176f9-8d80-4171-af1c-640eaaf80174/_boards/board/t/e95d0732-ca58-43d3-b205-fcf1f5e77ec9/Stories/)

[![Board Status](https://dev.azure.com/zbw-it27-group05/39e176f9-8d80-4171-af1c-640eaaf80174/e95d0732-ca58-43d3-b205-fcf1f5e77ec9/_apis/work/boardbadge/9dcfd190-a035-4b2c-a2ed-fef0d36b75a7)](https://dev.azure.com/zbw-it27-group05/39e176f9-8d80-4171-af1c-640eaaf80174/_boards/board/t/e95d0732-ca58-43d3-b205-fcf1f5e77ec9/Stories/)

[![Board Status](https://dev.azure.com/zbw-it27-group05/39e176f9-8d80-4171-af1c-640eaaf80174/e95d0732-ca58-43d3-b205-fcf1f5e77ec9/_apis/work/boardbadge/9dcfd190-a035-4b2c-a2ed-fef0d36b75a7)](https://dev.azure.com/zbw-it27-group05/39e176f9-8d80-4171-af1c-640eaaf80174/_boards/board/t/e95d0732-ca58-43d3-b205-fcf1f5e77ec9/Stories/)

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

•	Set secret:

replace . in Server=. with your Server

```ps

dotnet user-secrets set "ConnectionStrings:OrderManagement" "Server=.;Database=OrderManagement;Trusted_Connection=true;TrustServerCertificate=true;" --project 'C\Here\Must\Be\Your\Path\To\OrderManagement.Infrastructure.csproj'

```
