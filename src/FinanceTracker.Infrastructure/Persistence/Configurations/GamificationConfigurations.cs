using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FinanceTracker.Domain.Entities;

namespace FinanceTracker.Infrastructure.Persistence.Configurations;

public class UserFinancialProfileConfiguration : IEntityTypeConfiguration<UserFinancialProfile>
{
    public void Configure(EntityTypeBuilder<UserFinancialProfile> builder)
    {
        builder.ToTable("UserFinancialProfiles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired().HasMaxLength(256);
        builder.Property(x => x.Tier).HasMaxLength(20).HasDefaultValue("Bronze");
        builder.Property(x => x.HealthGrade).HasMaxLength(20);
        builder.Property(x => x.MonthlyBudgetAdherence).HasColumnType("decimal(5,2)");
        builder.Property(x => x.SavingsRate).HasColumnType("decimal(5,2)");
        builder.Property(x => x.DebtToIncomeRatio).HasColumnType("decimal(5,2)");
        builder.HasIndex(x => x.UserId).IsUnique();
    }
}

public class AchievementConfiguration : IEntityTypeConfiguration<Achievement>
{
    public void Configure(EntityTypeBuilder<Achievement> builder)
    {
        builder.ToTable("Achievements");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.Icon).HasMaxLength(10);
        builder.Property(x => x.Category).HasMaxLength(50);
        builder.Property(x => x.Difficulty).HasMaxLength(20);
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

public class UserAchievementConfiguration : IEntityTypeConfiguration<UserAchievement>
{
    public void Configure(EntityTypeBuilder<UserAchievement> builder)
    {
        builder.ToTable("UserAchievements");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired().HasMaxLength(256);
        builder.HasOne(x => x.Achievement).WithMany().HasForeignKey(x => x.AchievementId);
        builder.HasIndex(x => new { x.UserId, x.AchievementId }).IsUnique();
    }
}

public class PointTransactionConfiguration : IEntityTypeConfiguration<PointTransaction>
{
    public void Configure(EntityTypeBuilder<PointTransaction> builder)
    {
        builder.ToTable("PointTransactions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired().HasMaxLength(256);
        builder.Property(x => x.Reason).HasMaxLength(200);
        builder.Property(x => x.Category).HasMaxLength(50);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.EarnedAt);
    }
}

public class SavingsGoalConfiguration : IEntityTypeConfiguration<SavingsGoal>
{
    public void Configure(EntityTypeBuilder<SavingsGoal> builder)
    {
        builder.ToTable("SavingsGoals");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired().HasMaxLength(256);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Icon).HasMaxLength(10);
        builder.Property(x => x.Color).HasMaxLength(20);
        builder.Property(x => x.TargetAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.CurrentAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.MonthlyContribution).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Priority).HasMaxLength(20);
        builder.Property(x => x.Status).HasMaxLength(20);
        builder.HasIndex(x => x.UserId);
    }
}

public class SavingsDepositConfiguration : IEntityTypeConfiguration<SavingsDeposit>
{
    public void Configure(EntityTypeBuilder<SavingsDeposit> builder)
    {
        builder.ToTable("SavingsDeposits");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired().HasMaxLength(256);
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Note).HasMaxLength(500);
        builder.HasOne(x => x.SavingsGoal).WithMany().HasForeignKey(x => x.SavingsGoalId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.UserId);
    }
}

public class SavingsChallengeConfiguration : IEntityTypeConfiguration<SavingsChallenge>
{
    public void Configure(EntityTypeBuilder<SavingsChallenge> builder)
    {
        builder.ToTable("SavingsChallenges");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired().HasMaxLength(256);
        builder.Property(x => x.Type).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Status).HasMaxLength(20);
        builder.Property(x => x.TargetAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.CurrentAmount).HasColumnType("decimal(18,2)");
        builder.HasIndex(x => x.UserId);
    }
}

public class FinancialTipConfiguration : IEntityTypeConfiguration<FinancialTip>
{
    public void Configure(EntityTypeBuilder<FinancialTip> builder)
    {
        builder.ToTable("FinancialTips");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Content).IsRequired().HasMaxLength(2000);
        builder.Property(x => x.Category).HasMaxLength(50);
        builder.Property(x => x.Difficulty).HasMaxLength(20);
        builder.Property(x => x.SourceUrl).HasMaxLength(500);
    }
}
