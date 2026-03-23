using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FinanceTracker.Domain.Entities;

namespace FinanceTracker.Infrastructure.Persistence.Configurations;

public class StockWatchlistItemConfiguration : IEntityTypeConfiguration<StockWatchlistItem>
{
    public void Configure(EntityTypeBuilder<StockWatchlistItem> builder)
    {
        builder.ToTable("StockWatchlistItems");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired().HasMaxLength(256);
        builder.Property(x => x.Symbol).IsRequired().HasMaxLength(20);
        builder.Property(x => x.Exchange).HasMaxLength(20);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.AlertPriceAbove).HasColumnType("decimal(18,4)");
        builder.Property(x => x.AlertPriceBelow).HasColumnType("decimal(18,4)");
        builder.Property(x => x.Notes).HasMaxLength(500);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.UserId, x.Symbol }).IsUnique();
    }
}

public class CryptoWatchlistItemConfiguration : IEntityTypeConfiguration<CryptoWatchlistItem>
{
    public void Configure(EntityTypeBuilder<CryptoWatchlistItem> builder)
    {
        builder.ToTable("CryptoWatchlistItems");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired().HasMaxLength(256);
        builder.Property(x => x.CoinId).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Symbol).IsRequired().HasMaxLength(20);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.HoldingQuantity).HasColumnType("decimal(18,8)");
        builder.Property(x => x.AverageBuyPrice).HasColumnType("decimal(18,4)");
        builder.Property(x => x.Currency).HasMaxLength(10).HasDefaultValue("USD");
        builder.Property(x => x.AlertPriceAbove).HasColumnType("decimal(18,4)");
        builder.Property(x => x.AlertPriceBelow).HasColumnType("decimal(18,4)");
        builder.Property(x => x.Notes).HasMaxLength(500);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.UserId, x.CoinId }).IsUnique();
    }
}

public class CurrencyConversionConfiguration : IEntityTypeConfiguration<CurrencyConversion>
{
    public void Configure(EntityTypeBuilder<CurrencyConversion> builder)
    {
        builder.ToTable("CurrencyConversions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired().HasMaxLength(256);
        builder.Property(x => x.FromCurrency).IsRequired().HasMaxLength(10);
        builder.Property(x => x.ToCurrency).IsRequired().HasMaxLength(10);
        builder.Property(x => x.Amount).HasColumnType("decimal(18,4)");
        builder.Property(x => x.ConvertedAmount).HasColumnType("decimal(18,4)");
        builder.Property(x => x.ExchangeRate).HasColumnType("decimal(18,8)");
        builder.Property(x => x.Provider).HasMaxLength(100);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.ConvertedAt);
    }
}

public class TaxCalculationConfiguration : IEntityTypeConfiguration<TaxCalculation>
{
    public void Configure(EntityTypeBuilder<TaxCalculation> builder)
    {
        builder.ToTable("TaxCalculations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired().HasMaxLength(256);
        builder.Property(x => x.TaxYear).IsRequired().HasMaxLength(20);
        builder.Property(x => x.Country).HasMaxLength(10);
        builder.Property(x => x.GrossIncome).HasColumnType("decimal(18,2)");
        builder.Property(x => x.TaxableIncome).HasColumnType("decimal(18,2)");
        builder.Property(x => x.TaxAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.EffectiveRate).HasColumnType("decimal(5,2)");
        builder.Property(x => x.MedicalAidCredits).HasColumnType("decimal(18,2)");
        builder.Property(x => x.RetirementDeduction).HasColumnType("decimal(18,2)");

        builder.HasIndex(x => x.UserId);
    }
}

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired().HasMaxLength(256);
        builder.Property(x => x.InvoiceNumber).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(20);

        // Sender
        builder.Property(x => x.FromName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.FromEmail).HasMaxLength(200);
        builder.Property(x => x.FromAddress).HasMaxLength(500);
        builder.Property(x => x.FromVatNumber).HasMaxLength(50);

        // Recipient
        builder.Property(x => x.ToName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.ToEmail).HasMaxLength(200);
        builder.Property(x => x.ToAddress).HasMaxLength(500);
        builder.Property(x => x.ToVatNumber).HasMaxLength(50);

        // Financials
        builder.Property(x => x.Currency).HasMaxLength(10).HasDefaultValue("ZAR");
        builder.Property(x => x.Subtotal).HasColumnType("decimal(18,2)");
        builder.Property(x => x.VatRate).HasColumnType("decimal(5,2)");
        builder.Property(x => x.VatAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Total).HasColumnType("decimal(18,2)");
        builder.Property(x => x.DiscountPercentage).HasColumnType("decimal(5,2)");
        builder.Property(x => x.DiscountAmount).HasColumnType("decimal(18,2)");

        // Banking
        builder.Property(x => x.BankName).HasMaxLength(100);
        builder.Property(x => x.AccountHolder).HasMaxLength(200);
        builder.Property(x => x.AccountNumber).HasMaxLength(50);
        builder.Property(x => x.BranchCode).HasMaxLength(20);
        builder.Property(x => x.Reference).HasMaxLength(100);
        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.UserId, x.InvoiceNumber }).IsUnique();
        builder.HasIndex(x => x.Status);

        builder.HasMany(x => x.LineItems)
            .WithOne(x => x.Invoice)
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class InvoiceLineItemConfiguration : IEntityTypeConfiguration<InvoiceLineItem>
{
    public void Configure(EntityTypeBuilder<InvoiceLineItem> builder)
    {
        builder.ToTable("InvoiceLineItems");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Quantity).HasColumnType("decimal(10,2)");
        builder.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
        builder.Property(x => x.LineTotal).HasColumnType("decimal(18,2)");
    }
}
