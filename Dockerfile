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

RUN dotnet publish "src/OrderManagement.Presentation.Blazor/OrderManagement.Presentation.Blazor.csproj" \
    --configuration Release \
    --no-restore \
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
