# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["global.json", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
COPY [".editorconfig", "./"]
COPY ["src/OrderManagement.Presentation.Blazor/OrderManagement.Presentation.Blazor.csproj", "src/OrderManagement.Presentation.Blazor/"]
COPY ["src/OrderManagement.Application/OrderManagement.Application.csproj", "src/OrderManagement.Application/"]
COPY ["src/OrderManagement.Infrastructure/OrderManagement.Infrastructure.csproj", "src/OrderManagement.Infrastructure/"]
COPY ["src/OrderManagement.Domain/OrderManagement.Domain.csproj", "src/OrderManagement.Domain/"]
COPY ["src/SharedKernel/SharedKernel.csproj", "src/SharedKernel/"]

RUN dotnet restore "src/OrderManagement.Presentation.Blazor/OrderManagement.Presentation.Blazor.csproj"

COPY src/OrderManagement.Presentation.Blazor/ src/OrderManagement.Presentation.Blazor/
COPY src/OrderManagement.Application/ src/OrderManagement.Application/
COPY src/OrderManagement.Infrastructure/ src/OrderManagement.Infrastructure/
COPY src/OrderManagement.Domain/ src/OrderManagement.Domain/
COPY src/SharedKernel/ src/SharedKernel/

# No --no-restore here on purpose: the earlier restore ran before wwwroot/ was copied in, so its
# cached static web assets manifest doesn't yet know about Blazor's own framework assets
# (_framework/blazor.web.js) - restoring again now, with the full source present, is fast
# (packages are already cached) and produces a correct manifest.
RUN dotnet publish "src/OrderManagement.Presentation.Blazor/OrderManagement.Presentation.Blazor.csproj" \
    --configuration Release \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN groupadd --system --gid 1001 appgroup \
    && useradd --system --uid 1001 --gid appgroup --no-create-home --shell /sbin/nologin appuser

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

USER appuser

ENTRYPOINT ["dotnet", "OrderManagement.Presentation.Blazor.dll"]

# Test-runner stage: runs the full automated test suite (architecture, SharedKernel, Domain,
# Application, bUnit, Infrastructure integration, Reqnroll acceptance and Playwright E2E tests)
# without requiring a local .NET SDK, SQL Server or Playwright browser install. The Playwright
# .NET image tag is pinned to the exact Microsoft.Playwright.MSTest NuGet version in
# Directory.Packages.props - Playwright requires the Docker image and package version to match,
# and it already bundles a compatible .NET SDK and the Chromium browser + OS dependencies.
FROM mcr.microsoft.com/playwright/dotnet:v1.62.0-noble AS test-runner
WORKDIR /src

COPY . .

RUN dotnet restore OrderManagement.sln
RUN dotnet build OrderManagement.sln --configuration Release --no-restore

ENV TEST_PROJECTS="OrderManagement.sln"

ENTRYPOINT ["/bin/sh", "-c", "dotnet test $TEST_PROJECTS --configuration Release --no-build --logger trx --results-directory /testresults"]
