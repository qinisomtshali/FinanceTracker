using FinanceTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceTracker.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core Fluent API configuration for Category.
/// 
/// Each entity gets its own configuration class. This keeps things organized
/// and follows Single Responsibility — each class configures one table.
/// </summary>
public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Icon)
            .HasMaxLength(50); 

        // Index for faster lookups: "Get all categories for user X"
        builder.HasIndex(c => c.UserId);

        // Composite unique index: No duplicate category names per user
        builder.HasIndex(c => new { c.UserId, c.Name }).IsUnique();
    }
}

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(t => t.Id);

        // decimal(18,2) = 18 total digits, 2 after decimal point
        // Standard for financial data — avoids floating-point precision issues
        builder.Property(t => t.Amount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(t => t.Description)
            .HasMaxLength(500);

        builder.HasOne(t => t.Category)
            .WithMany(c => c.Transactions)
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Restrict); // Don't cascade — prevent accidental data loss

        // Indexes for common queries
        builder.HasIndex(t => t.UserId);
        builder.HasIndex(t => t.Date);
        builder.HasIndex(t => new { t.UserId, t.Date }); // "Get user's transactions for date range"
    }
}

public class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Amount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.HasOne(b => b.Category)
            .WithMany(c => c.Budgets)
            .HasForeignKey(b => b.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // One budget per category per month per user
        builder.HasIndex(b => new { b.UserId, b.CategoryId, b.Month, b.Year }).IsUnique();
    }
}


