# ============================================================
# MULTI-STAGE DOCKERFILE
# 
# WHY MULTI-STAGE: We use one image to BUILD the app (has SDK, 
# compilers, NuGet) and a different, smaller image to RUN it 
# (only has the runtime). This keeps the final image small:
#   - Build image: ~800MB (has SDK)
#   - Final image: ~100MB (runtime only)
#
# A smaller image means:
#   1. Faster deployments (less to upload)
#   2. Smaller attack surface (less software = fewer vulnerabilities)
#   3. Lower storage costs in container registries
# ============================================================

# ---- STAGE 1: BUILD ----
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy solution and project files first (for layer caching)
# Docker caches each layer. If project files haven't changed,
# the restore step is skipped on subsequent builds — huge time saver.
COPY FinanceTracker.sln .
COPY src/FinanceTracker.Domain/FinanceTracker.Domain.csproj src/FinanceTracker.Domain/
COPY src/FinanceTracker.Application/FinanceTracker.Application.csproj src/FinanceTracker.Application/
COPY src/FinanceTracker.Infrastructure/FinanceTracker.Infrastructure.csproj src/FinanceTracker.Infrastructure/
COPY src/FinanceTracker.API/FinanceTracker.API.csproj src/FinanceTracker.API/
COPY tests/FinanceTracker.UnitTests/FinanceTracker.UnitTests.csproj tests/FinanceTracker.UnitTests/
COPY tests/FinanceTracker.IntegrationTests/FinanceTracker.IntegrationTests.csproj tests/FinanceTracker.IntegrationTests/

# Restore NuGet packages (cached unless .csproj files change)
RUN dotnet restore

# Copy everything else and build
COPY . .
RUN dotnet publish src/FinanceTracker.API/FinanceTracker.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ---- STAGE 2: RUNTIME ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app

# Create a non-root user for security
# NEVER run containers as root in production
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy the published app from the build stage
COPY --from=build /app/publish .

# Set the non-root user
USER appuser

# Expose port 8080 (ASP.NET Core's default in containers)
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Start the application
ENTRYPOINT ["dotnet", "FinanceTracker.API.dll"]
