# Multi-stage Dockerfile for TradingPlatform.Worker (.NET 10 LTS)
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /app

# Copy solution and project files
COPY TradingPlatform.slnx ./
COPY src/TradingPlatform.Domain/*.csproj ./src/TradingPlatform.Domain/
COPY src/TradingPlatform.Contracts/*.csproj ./src/TradingPlatform.Contracts/
COPY src/TradingPlatform.Application/*.csproj ./src/TradingPlatform.Application/
COPY src/TradingPlatform.Infrastructure/*.csproj ./src/TradingPlatform.Infrastructure/
COPY src/TradingPlatform.Worker/*.csproj ./src/TradingPlatform.Worker/

RUN dotnet restore src/TradingPlatform.Worker/TradingPlatform.Worker.csproj

# Copy remaining source code
COPY src/TradingPlatform.Domain/ ./src/TradingPlatform.Domain/
COPY src/TradingPlatform.Contracts/ ./src/TradingPlatform.Contracts/
COPY src/TradingPlatform.Application/ ./src/TradingPlatform.Application/
COPY src/TradingPlatform.Infrastructure/ ./src/TradingPlatform.Infrastructure/
COPY src/TradingPlatform.Worker/ ./src/TradingPlatform.Worker/

WORKDIR /app/src/TradingPlatform.Worker
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime Image
FROM mcr.microsoft.com/dotnet/runtime:10.0-preview AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV DOTNET_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "TradingPlatform.Worker.dll"]
