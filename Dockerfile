# ---- STAGE 1: BUILD ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY FinanceTracker.sln .
COPY src/FinanceTracker.Domain/FinanceTracker.Domain.csproj src/FinanceTracker.Domain/
COPY src/FinanceTracker.Application/FinanceTracker.Application.csproj src/FinanceTracker.Application/
COPY src/FinanceTracker.Infrastructure/FinanceTracker.Infrastructure.csproj src/FinanceTracker.Infrastructure/
COPY src/FinanceTracker.API/FinanceTracker.API.csproj src/FinanceTracker.API/
COPY tests/FinanceTracker.UnitTests/FinanceTracker.UnitTests.csproj tests/FinanceTracker.UnitTests/
COPY tests/FinanceTracker.IntegrationTests/FinanceTracker.IntegrationTests.csproj tests/FinanceTracker.IntegrationTests/

RUN dotnet restore

COPY . .
RUN dotnet publish src/FinanceTracker.API/FinanceTracker.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ---- STAGE 2: RUNTIME ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "FinanceTracker.API.dll"]