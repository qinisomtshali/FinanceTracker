using FinanceTracker.API.Middleware;
using FinanceTracker.API.Extensions;
using FinanceTracker.Application;
using FinanceTracker.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Serilog;

// ============================================================
// PROGRAM.CS — The entry point of the entire application.
// This is where all the layers come together.
//
// Think of this as the COMPOSITION ROOT — the one place where
// we wire up all dependencies. After this, every class gets its
// dependencies via constructor injection automatically.
// ============================================================

var builder = WebApplication.CreateBuilder(args);

// ---- Serilog: Structured logging ----
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// ---- Register services from each layer ----
// Each layer registers its own services — the API layer just calls them.
builder.Services.AddApplication();                                    // MediatR, FluentValidation
builder.Services.AddInfrastructure(builder.Configuration);            // EF Core, Identity, Repos
builder.Services.AddApiServices(builder.Configuration);               // JWT, Swagger, CORS, CurrentUser

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// ---- Middleware pipeline ----
// Order matters! Each request flows through these in sequence.

// Global exception handling — catches unhandled exceptions and returns clean JSON
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FinanceTracker API v1");
        c.RoutePrefix = string.Empty; // Swagger at root URL
    });
}

app.UseHttpsRedirection();

// CORS must come before auth
app.UseCors("AllowFrontend");

// Authentication then Authorization — order is critical
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Log startup
Log.Information("FinanceTracker API starting on {Environment}", app.Environment.EnvironmentName);

// Auto-migrate database on startup (for Docker/deployment)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FinanceTracker.Infrastructure.Data.ApplicationDbContext>();
    db.Database.Migrate();
}

app.Run();
