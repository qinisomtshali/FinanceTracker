using FinanceTracker.Domain.Entities;
using FinanceTracker.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Infrastructure.Data;

/// <summary>
/// The database context — EF Core's representation of your database.
/// 
/// Inherits from IdentityDbContext so we get all the Identity tables
/// (AspNetUsers, AspNetRoles, etc.) automatically, PLUS our custom tables.
/// 
/// DbSet properties = database tables. EF Core uses these to generate
/// SQL queries and track changes.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Budget> Budgets => Set<Budget>();

    public DbSet<StockWatchlistItem> StockWatchlistItems => Set<StockWatchlistItem>();
    public DbSet<CryptoWatchlistItem> CryptoWatchlistItems => Set<CryptoWatchlistItem>();
    public DbSet<CurrencyConversion> CurrencyConversions => Set<CurrencyConversion>();
    public DbSet<TaxCalculation> TaxCalculations => Set<TaxCalculation>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();

    /// <summary>
    /// Configure entity relationships, constraints, and indexes.
    /// This is called once when EF Core builds its internal model.
    /// 
    /// We use the Fluent API here instead of Data Annotations because:
    ///   1. Domain entities stay clean (no EF attributes polluting them)
    ///   2. All configuration is in one place per entity
    ///   3. More powerful — some configurations can only be done via Fluent API
    /// </summary>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Ignore the Domain User entity — we use ApplicationUser (Identity) instead.
        // Relationships use UserId (Guid) as a simple FK without navigating to User.
        builder.Ignore<Domain.Entities.User>();

        // Apply all entity configurations from this assembly
        // (any class implementing IEntityTypeConfiguration<T>)
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

    }

    /// <summary>
    /// Automatically set UpdatedAt timestamp on modified entities.
    /// This fires on every SaveChanges call — no manual tracking needed.
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<Domain.Common.BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
