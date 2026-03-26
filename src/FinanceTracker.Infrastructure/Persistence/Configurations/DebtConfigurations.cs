using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FinanceTracker.Domain.Entities;

namespace FinanceTracker.Infrastructure.Persistence.Configurations;

public class DebtConfiguration : IEntityTypeConfiguration<Debt>
{
    public void Configure(EntityTypeBuilder<Debt> builder)
    {
        builder.ToTable("Debts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired().HasMaxLength(256);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Type).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Lender).HasMaxLength(100);
        builder.Property(x => x.OriginalAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.CurrentBalance).HasColumnType("decimal(18,2)");
        builder.Property(x => x.InterestRate).HasColumnType("decimal(5,2)");
        builder.Property(x => x.MinimumPayment).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ActualPayment).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("Active");
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.HasIndex(x => x.UserId);
        builder.HasMany(x => x.Payments).WithOne(x => x.Debt).HasForeignKey(x => x.DebtId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class DebtPaymentConfiguration : IEntityTypeConfiguration<DebtPayment>
{
    public void Configure(EntityTypeBuilder<DebtPayment> builder)
    {
        builder.ToTable("DebtPayments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired().HasMaxLength(256);
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.BalanceAfter).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Note).HasMaxLength(500);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.DebtId);
    }
}
