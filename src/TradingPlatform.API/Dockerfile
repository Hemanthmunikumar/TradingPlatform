# Multi-stage Dockerfile for TradingPlatform.API (.NET 10 LTS - Multi-Arch AMD64 & ARM64)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy solution and project files first for optimal layer caching
COPY TradingPlatform.slnx ./
COPY src/TradingPlatform.Domain/*.csproj ./src/TradingPlatform.Domain/
COPY src/TradingPlatform.Contracts/*.csproj ./src/TradingPlatform.Contracts/
COPY src/TradingPlatform.Application/*.csproj ./src/TradingPlatform.Application/
COPY src/TradingPlatform.Infrastructure/*.csproj ./src/TradingPlatform.Infrastructure/
COPY src/TradingPlatform.API/*.csproj ./src/TradingPlatform.API/

RUN dotnet restore src/TradingPlatform.API/TradingPlatform.API.csproj

# Copy remaining source code
COPY src/TradingPlatform.Domain/ ./src/TradingPlatform.Domain/
COPY src/TradingPlatform.Contracts/ ./src/TradingPlatform.Contracts/
COPY src/TradingPlatform.Application/ ./src/TradingPlatform.Application/
COPY src/TradingPlatform.Infrastructure/ ./src/TradingPlatform.Infrastructure/
COPY src/TradingPlatform.API/ ./src/TradingPlatform.API/

WORKDIR /app/src/TradingPlatform.API
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime Image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install Python 3 (for india-day-trader-skill scanners) and curl (for container healthcheck)
RUN apt-get update \
    && apt-get install -y --no-install-recommends python3 curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .
COPY src/india-day-trader-skill/ ./src/india-day-trader-skill/

ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 5000

ENTRYPOINT ["dotnet", "TradingPlatform.API.dll"]
