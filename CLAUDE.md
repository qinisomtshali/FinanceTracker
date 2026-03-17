# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Restore dependencies
dotnet restore

# Run the API (Swagger UI at https://localhost:5001)
cd src/FinanceTracker.API && dotnet run

# Run all tests
dotnet test

# Run a single test project
dotnet test tests/FinanceTracker.UnitTests
dotnet test tests/FinanceTracker.IntegrationTests

# Run tests with filter
dotnet test --filter "FullyQualifiedName~CategoryTests"

# Build
dotnet build

# Add a new EF Core migration
cd src/FinanceTracker.Infrastructure
dotnet ef migrations add <MigrationName> --startup-project ../FinanceTracker.API

# Apply migrations
dotnet ef database update --startup-project ../FinanceTracker.API
```

## Architecture

Clean Architecture with CQRS. Dependencies point strictly inward:

```
API → Application → Domain
Infrastructure → Application → Domain
```

**Domain** (`src/FinanceTracker.Domain`) — Zero external dependencies. Contains entities (`Transaction`, `Budget`, `Category`, `User`), enums, and interfaces. All entities inherit `BaseEntity` (provides `Id`, `CreatedAt`, `UpdatedAt`).

**Application** (`src/FinanceTracker.Application`) — Business logic only. Each feature lives under `Features/<Feature>/` and contains:
- A `*Command` or `*Query` record (implements `IRequest<Result<T>>`)
- A `*Handler` class (implements `IRequestHandler`)
- A `*Validator` class (FluentValidation)

MediatR dispatches commands/queries to their handlers automatically. `ValidationBehavior` (a MediatR pipeline behavior) runs all validators before a handler executes — no manual validation in handlers.

Operations return `Result<T>` (not exceptions) for expected business failures. Only unexpected failures (infra errors) throw exceptions.

**Infrastructure** (`src/FinanceTracker.Infrastructure`) — EF Core, ASP.NET Identity, repository implementations. Houses all DB migrations.

**API** (`src/FinanceTracker.API`) — Thin controllers that translate HTTP ↔ MediatR commands/queries. `Program.cs` is the composition root — each layer registers its own DI via `AddApplication()`, `AddInfrastructure()`, `AddApiServices()`. Middleware order: `GlobalExceptionMiddleware` → CORS → Authentication → Authorization.

## Adding a New Feature

Follow this pattern (see `Categories` as reference):

1. Add DTO to `Application/DTOs/<Feature>/`
2. Add Command/Query record + Handler + Validator to `Application/Features/<Feature>/`
3. Add repository interface to `Domain/Interfaces/` if needed
4. Implement repository in `Infrastructure/`
5. Add controller to `API/Controllers/`

## Key Conventions

- All entities use `Guid` PKs
- Timestamps are UTC (`DateTime.UtcNow`)
- Business logic errors → `Result<T>.Failure(...)`, not exceptions
- Controllers map `Result.IsSuccess` to HTTP status codes (200/400/404/etc.)
- `ICurrentUserService` injects the authenticated user's ID into handlers
