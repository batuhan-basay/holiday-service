# syntax=docker/dockerfile:1

# ---- build ---------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore first so the layer is cached while only source changes.
COPY Directory.Build.props Directory.Packages.props HolidayService.sln ./
COPY src/HolidayService.Domain/*.csproj src/HolidayService.Domain/
COPY src/HolidayService.Application/*.csproj src/HolidayService.Application/
COPY src/HolidayService.Infrastructure/*.csproj src/HolidayService.Infrastructure/
COPY src/HolidayService.Api/*.csproj src/HolidayService.Api/
COPY tests/HolidayService.UnitTests/*.csproj tests/HolidayService.UnitTests/
COPY tests/HolidayService.IntegrationTests/*.csproj tests/HolidayService.IntegrationTests/
RUN dotnet restore HolidayService.sln

COPY . .
RUN dotnet publish src/HolidayService.Api/HolidayService.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ---- runtime -------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Run as the non-root user the base image ships with.
USER $APP_UID

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_TieredPGO=1

EXPOSE 8080

COPY --from=build /app/publish .

# No HEALTHCHECK here on purpose: the aspnet image ships no HTTP client, so a
# meaningful probe belongs to the orchestrator. Point it at GET /health.

ENTRYPOINT ["dotnet", "HolidayService.Api.dll"]
