# FinanceTracker API

A personal finance tracker built with **ASP.NET Core 10** following **Clean Architecture** principles.

## Architecture

```
src/
├── FinanceTracker.Domain          → Entities, Enums, Interfaces (zero dependencies)
├── FinanceTracker.Application     → Use cases, DTOs, Validators, MediatR handlers
├── FinanceTracker.Infrastructure  → EF Core, Identity, Repository implementations
└── FinanceTracker.API             → Controllers, Middleware, Program.cs
```

**Dependency rule:** Dependencies point inward. Domain has no references. API references everything.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (LocalDB, Express, or full)
- [Git](https://git-scm.com/)

## Getting Started

### 1. Clone and restore
```bash
git clone <your-repo-url>
cd FinanceTracker
dotnet restore
```

### 2. Update the connection string
Edit `src/FinanceTracker.API/appsettings.json` and set your SQL Server connection string.

### 3. Create the database
```bash
cd src/FinanceTracker.Infrastructure
dotnet ef migrations add InitialCreate --startup-project ../FinanceTracker.API
dotnet ef database update --startup-project ../FinanceTracker.API
```

### 4. Run the API
```bash
cd src/FinanceTracker.API
dotnet run
```

### 5. Open Swagger
Navigate to `https://localhost:5001` (or the port shown in terminal) to see the API docs.

## Tech Stack

| Layer          | Technology                              |
|----------------|----------------------------------------|
| API            | ASP.NET Core 10 Web API                |
| Architecture   | Clean Architecture + CQRS (MediatR)    |
| Database       | SQL Server + Entity Framework Core 10  |
| Auth           | ASP.NET Identity + JWT Bearer          |
| Validation     | FluentValidation                       |
| Logging        | Serilog                                |
| Testing        | xUnit + Moq + FluentAssertions        |
| Docs           | Swagger / OpenAPI                      |

## Project Status

- [x] Phase 1: Planning
- [x] Phase 2: Requirements & Design
- [x] Phase 3: Project Scaffolding
- [ ] Phase 4: Build features iteratively
- [ ] Phase 5: Testing
- [ ] Phase 6: CI/CD (GitHub Actions)
- [ ] Phase 7: Docker
- [ ] Phase 8: Deploy to Azure
- [ ] Phase 9: Kubernetes
- [ ] Phase 10: Security hardening
