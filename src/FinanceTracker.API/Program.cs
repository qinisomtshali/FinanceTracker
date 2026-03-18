using FinanceTracker.API.Middleware;
using FinanceTracker.API.Extensions;
using FinanceTracker.Application;
using FinanceTracker.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

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

// ---- Rate Limiting ----
// Prevents brute force attacks and API abuse.
// Fixed window: max 100 requests per 1-minute window per user.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // General rate limit for all endpoints
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });

    // Strict rate limit for auth endpoints (prevents brute force)
    options.AddFixedWindowLimiter("auth", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// ---- Middleware pipeline ----
// Order matters! Each request flows through these in sequence.

// Global exception handling — catches unhandled exceptions and returns clean JSON
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FinanceTracker API v1");
        c.RoutePrefix = string.Empty; // Swagger at root URL
    });
}

if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

// CORS must come before auth
app.UseCors("AllowFrontend");

app.UseRateLimiter();

// Authentication then Authorization — order is critical
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Log startup
Log.Information("FinanceTracker API starting on {Environment}", app.Environment.EnvironmentName);

// Auto-create/migrate database on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FinanceTracker.Infrastructure.Data.ApplicationDbContext>();
    db.Database.EnsureCreated();
}

app.Run();
