using FinanceTracker.Application.Features.Debts;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Enums;
using FinanceTracker.Domain.Services;
using FinanceTracker.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;

namespace FinanceTracker.Infrastructure.Handlers;

public class GetDebtsHandler : IRequestHandler<GetDebtsQuery, List<DebtDto>>
{
    private readonly ApplicationDbContext _db;
    public GetDebtsHandler(ApplicationDbContext db) => _db = db;

    public async Task<List<DebtDto>> Handle(GetDebtsQuery request, CancellationToken ct)
    {
        var debts = await _db.Set<Debt>()
            .Where(d => d.UserId == request.UserId)
            .Include(d => d.Payments)
            .OrderBy(d => d.Status == "PaidOff" ? 1 : 0)
            .ThenBy(d => d.CurrentBalance)
            .ToListAsync(ct);

        return debts.Select(MapToDto).ToList();
    }

    internal static DebtDto MapToDto(Debt d)
    {
        var totalPaid = d.OriginalAmount - d.CurrentBalance;
        var pctPaid = d.OriginalAmount > 0 ? Math.Round(totalPaid / d.OriginalAmount * 100, 1) : 0;
        int? monthsToPayoff = null;

        if (d.CurrentBalance > 0 && d.ActualPayment > 0)
        {
            var monthlyRate = d.InterestRate / 100 / 12;
            if (monthlyRate > 0)
            {
                var monthlyInterest = d.CurrentBalance * monthlyRate;
                if (d.ActualPayment > monthlyInterest)
                {
                    var r = (double)monthlyRate;
                    var balance = (double)d.CurrentBalance;
                    var payment = (double)d.ActualPayment;
                    var logArg = 1 - (r * balance / payment);
                    if (logArg > 0)
                        monthsToPayoff = (int)Math.Ceiling(-Math.Log(logArg) / Math.Log(1 + r));
                }
            }
            else
            {
                monthsToPayoff = (int)Math.Ceiling(d.CurrentBalance / d.ActualPayment);
            }
        }

        return new DebtDto(
            d.Id, d.Name, d.Type, d.Lender,
            d.OriginalAmount, d.CurrentBalance, d.InterestRate,
            d.MinimumPayment, d.ActualPayment, d.DueDay,
            d.StartDate, d.Status, d.Notes,
            pctPaid, totalPaid, monthsToPayoff, d.CreatedAt
        );
    }
}

public class GetDebtByIdHandler : IRequestHandler<GetDebtByIdQuery, DebtDto?>
{
    private readonly ApplicationDbContext _db;
    public GetDebtByIdHandler(ApplicationDbContext db) => _db = db;

    public async Task<DebtDto?> Handle(GetDebtByIdQuery request, CancellationToken ct)
    {
        var debt = await _db.Set<Debt>()
            .Include(d => d.Payments)
            .FirstOrDefaultAsync(d => d.Id == request.DebtId && d.UserId == request.UserId, ct);
        return debt == null ? null : GetDebtsHandler.MapToDto(debt);
    }
}

public class CreateDebtHandler : IRequestHandler<CreateDebtCommand, DebtDto>
{
    private readonly ApplicationDbContext _db;
    public CreateDebtHandler(ApplicationDbContext db) => _db = db;

    public async Task<DebtDto> Handle(CreateDebtCommand request, CancellationToken ct)
    {
        var debt = new Debt
        {
            UserId = request.UserId,
            Name = request.Name,
            Type = request.Type,
            Lender = request.Lender,
            OriginalAmount = request.OriginalAmount,
            CurrentBalance = request.CurrentBalance,
            InterestRate = request.InterestRate,
            MinimumPayment = request.MinimumPayment,
            ActualPayment = request.ActualPayment > 0 ? request.ActualPayment : request.MinimumPayment,
            DueDay = request.DueDay,
            StartDate = request.StartDate,
            Notes = request.Notes
        };

        _db.Set<Debt>().Add(debt);
        await _db.SaveChangesAsync(ct);

        return GetDebtsHandler.MapToDto(debt);
    }
}

public class UpdateDebtHandler : IRequestHandler<UpdateDebtCommand, bool>
{
    private readonly ApplicationDbContext _db;
    public UpdateDebtHandler(ApplicationDbContext db) => _db = db;

    public async Task<bool> Handle(UpdateDebtCommand request, CancellationToken ct)
    {
        var debt = await _db.Set<Debt>()
            .FirstOrDefaultAsync(d => d.Id == request.DebtId && d.UserId == request.UserId, ct);
        if (debt == null) return false;

        if (request.Name != null) debt.Name = request.Name;
        if (request.Type != null) debt.Type = request.Type;
        if (request.Lender != null) debt.Lender = request.Lender;
        if (request.CurrentBalance.HasValue) debt.CurrentBalance = request.CurrentBalance.Value;
        if (request.InterestRate.HasValue) debt.InterestRate = request.InterestRate.Value;
        if (request.MinimumPayment.HasValue) debt.MinimumPayment = request.MinimumPayment.Value;
        if (request.ActualPayment.HasValue) debt.ActualPayment = request.ActualPayment.Value;
        if (request.DueDay.HasValue) debt.DueDay = request.DueDay.Value;
        if (request.Status != null) debt.Status = request.Status;
        if (request.Notes != null) debt.Notes = request.Notes;
        debt.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return true;
    }
}

public class DeleteDebtHandler : IRequestHandler<DeleteDebtCommand, bool>
{
    private readonly ApplicationDbContext _db;
    public DeleteDebtHandler(ApplicationDbContext db) => _db = db;

    public async Task<bool> Handle(DeleteDebtCommand request, CancellationToken ct)
    {
        var debt = await _db.Set<Debt>()
            .FirstOrDefaultAsync(d => d.Id == request.DebtId && d.UserId == request.UserId, ct);
        if (debt == null) return false;

        _db.Set<Debt>().Remove(debt);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

public class GetDebtSummaryHandler : IRequestHandler<GetDebtSummaryQuery, DebtSummaryDto>
{
    private readonly ApplicationDbContext _db;
    public GetDebtSummaryHandler(ApplicationDbContext db) => _db = db;

    public async Task<DebtSummaryDto> Handle(GetDebtSummaryQuery request, CancellationToken ct)
    {
        var debts = await _db.Set<Debt>()
            .Where(d => d.UserId == request.UserId)
            .ToListAsync(ct);

        var activeDebts = debts.Where(d => d.Status == "Active").ToList();
        var totalDebt = activeDebts.Sum(d => d.CurrentBalance);
        var totalOriginal = debts.Sum(d => d.OriginalAmount);
        var totalMonthly = activeDebts.Sum(d => d.ActualPayment);
        var totalPaidOff = totalOriginal - debts.Sum(d => d.CurrentBalance);
        var progress = totalOriginal > 0 ? Math.Round(totalPaidOff / totalOriginal * 100, 1) : 0;

        // Estimate debt-free date using current payments
        int? monthsToFree = null;
        DateTime? debtFreeDate = null;
        if (activeDebts.Any() && totalMonthly > 0)
        {
            var inputs = activeDebts.Select(d => new DebtInput
            {
                Name = d.Name,
                CurrentBalance = d.CurrentBalance,
                InterestRate = d.InterestRate,
                MinimumPayment = d.MinimumPayment,
                ActualPayment = d.ActualPayment
            }).ToList();

            var plan = DebtPayoffCalculator.CalculateCurrentPlan(inputs);
            monthsToFree = plan.TotalMonths;
            debtFreeDate = plan.DebtFreeDate;
        }

        // Debt-to-income ratio (need monthly income)
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var userId = Guid.Parse(request.UserId);
        var monthlyIncome = await _db.Transactions
            .Where(t => t.UserId == userId && t.Type == TransactionType.Income && t.Date >= monthStart)
            .SumAsync(t => t.Amount, ct);

        var dti = monthlyIncome > 0 ? Math.Round(totalMonthly / monthlyIncome * 100, 1) : 0;

        return new DebtSummaryDto(
            TotalDebt: totalDebt,
            TotalOriginalDebt: totalOriginal,
            TotalMonthlyPayments: totalMonthly,
            TotalPaidOff: totalPaidOff,
            OverallProgress: progress,
            ActiveDebts: activeDebts.Count,
            PaidOffDebts: debts.Count(d => d.Status == "PaidOff"),
            DebtToIncomeRatio: dti,
            EstimatedDebtFreeDate: debtFreeDate,
            EstimatedMonthsToFree: monthsToFree
        );
    }
}

public class GetDebtPaymentsHandler : IRequestHandler<GetDebtPaymentsQuery, List<DebtPaymentDto>>
{
    private readonly ApplicationDbContext _db;
    public GetDebtPaymentsHandler(ApplicationDbContext db) => _db = db;

    public async Task<List<DebtPaymentDto>> Handle(GetDebtPaymentsQuery request, CancellationToken ct)
    {
        return await _db.Set<DebtPayment>()
            .Where(p => p.DebtId == request.DebtId && p.UserId == request.UserId)
            .OrderByDescending(p => p.PaymentDate)
            .Select(p => new DebtPaymentDto(p.Id, p.Amount, p.BalanceAfter, p.Note, p.PaymentDate))
            .ToListAsync(ct);
    }
}

public class LogDebtPaymentHandler : IRequestHandler<LogDebtPaymentCommand, DebtPaymentResultDto>
{
    private readonly ApplicationDbContext _db;
    public LogDebtPaymentHandler(ApplicationDbContext db) => _db = db;

    public async Task<DebtPaymentResultDto> Handle(LogDebtPaymentCommand request, CancellationToken ct)
    {
        var debt = await _db.Set<Debt>()
            .FirstOrDefaultAsync(d => d.Id == request.DebtId && d.UserId == request.UserId, ct);

        if (debt == null) throw new InvalidOperationException("Debt not found");

        debt.CurrentBalance -= request.Amount;
        if (debt.CurrentBalance < 0) debt.CurrentBalance = 0;
        debt.UpdatedAt = DateTime.UtcNow;

        bool paidOff = false;
        if (debt.CurrentBalance <= 0.01m)
        {
            debt.CurrentBalance = 0;
            debt.Status = "PaidOff";
            paidOff = true;
        }

        var payment = new DebtPayment
        {
            DebtId = debt.Id,
            UserId = request.UserId,
            Amount = request.Amount,
            BalanceAfter = debt.CurrentBalance,
            Note = request.Note
        };

        _db.Set<DebtPayment>().Add(payment);

        // Gamification points
        int points = GamificationEngine.PointValues.MakeSavingsDeposit; // 10 pts for logging payment
        if (request.Amount > debt.MinimumPayment)
            points += 10; // bonus for paying above minimum
        if (paidOff)
            points += 100; // big bonus for paying off a debt

        var profile = await _db.Set<UserFinancialProfile>()
            .FirstOrDefaultAsync(p => p.UserId == request.UserId, ct);
        if (profile != null)
        {
            profile.TotalPoints += points;
            profile.UpdatedAt = DateTime.UtcNow;
        }

        _db.Set<PointTransaction>().Add(new PointTransaction
        {
            UserId = request.UserId,
            Points = points,
            Reason = paidOff ? $"Paid off: {debt.Name}!" : $"Debt payment: {debt.Name}",
            Category = "Debt"
        });

        await _db.SaveChangesAsync(ct);

        var pctPaid = debt.OriginalAmount > 0
            ? Math.Round((debt.OriginalAmount - debt.CurrentBalance) / debt.OriginalAmount * 100, 1)
            : 100;

        return new DebtPaymentResultDto(payment.Id, debt.CurrentBalance, pctPaid, paidOff, points);
    }
}

public class GetPayoffPlanHandler : IRequestHandler<GetPayoffPlanQuery, StrategyComparisonResult>
{
    private readonly ApplicationDbContext _db;
    public GetPayoffPlanHandler(ApplicationDbContext db) => _db = db;

    public async Task<StrategyComparisonResult> Handle(GetPayoffPlanQuery request, CancellationToken ct)
    {
        var debts = await _db.Set<Debt>()
            .Where(d => d.UserId == request.UserId && d.Status == "Active")
            .ToListAsync(ct);

        if (!debts.Any())
        {
            return new StrategyComparisonResult
            {
                ExtraMonthlyPayment = request.ExtraPayment,
                CurrentPlan = new PayoffPlanResult { Strategy = "Current" },
                Snowball = new PayoffPlanResult { Strategy = "Snowball" },
                Avalanche = new PayoffPlanResult { Strategy = "Avalanche" },
                RecommendedStrategy = "None",
                Summary = "No active debts found. You're debt-free!"
            };
        }

        var inputs = debts.Select(d => new DebtInput
        {
            Name = d.Name,
            CurrentBalance = d.CurrentBalance,
            InterestRate = d.InterestRate,
            MinimumPayment = d.MinimumPayment,
            ActualPayment = d.ActualPayment
        }).ToList();

        return DebtPayoffCalculator.CompareStrategies(inputs, request.ExtraPayment);
    }
}
