using FinanceTracker.Application.Features.Recurring;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Enums;
using FinanceTracker.Domain.Services;
using FinanceTracker.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Infrastructure.Handlers;

public class GetRecurringTransactionsHandler : IRequestHandler<GetRecurringTransactionsQuery, List<RecurringTransactionDto>>
{
    private readonly ApplicationDbContext _db;
    public GetRecurringTransactionsHandler(ApplicationDbContext db) => _db = db;

    public async Task<List<RecurringTransactionDto>> Handle(GetRecurringTransactionsQuery request, CancellationToken ct)
    {
        var userId = Guid.Parse(request.UserId);
        var items = await _db.Set<RecurringTransaction>()
            .Where(r => r.UserId == userId)
            .Include(r => r.Category)
            .OrderBy(r => r.DayOfMonth)
            .ToListAsync(ct);

        return items.Select(r => MapToDto(r)).ToList();
    }

    internal static RecurringTransactionDto MapToDto(RecurringTransaction r)
    {
        var nextDue = r.NextDueDate ?? BillScheduleCalculator.CalculateNextDueDate(
            r.Frequency, r.DayOfMonth, r.DayOfWeek, DateTime.UtcNow);

        return new RecurringTransactionDto(
            r.Id, r.Name, r.Amount, r.Description,
            r.Type.ToString(), r.CategoryId, r.Category?.Name,
            r.Frequency, r.DayOfMonth, r.DayOfWeek,
            r.StartDate, r.EndDate, nextDue, r.LastGeneratedDate,
            r.IsActive, r.AutoGenerate, r.NotifyBeforeDue, r.NotifyDaysBefore,
            r.CreatedAt
        );
    }
}

public class CreateRecurringTransactionHandler : IRequestHandler<CreateRecurringTransactionCommand, RecurringTransactionDto>
{
    private readonly ApplicationDbContext _db;
    public CreateRecurringTransactionHandler(ApplicationDbContext db) => _db = db;

    public async Task<RecurringTransactionDto> Handle(CreateRecurringTransactionCommand request, CancellationToken ct)
    {
        var userId = Guid.Parse(request.UserId);

        // Verify category belongs to user
        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId && c.UserId == userId, ct);
        if (category == null) throw new InvalidOperationException("Category not found");

        var nextDue = BillScheduleCalculator.CalculateNextDueDate(
            request.Frequency, request.DayOfMonth, request.DayOfWeek, DateTime.UtcNow);

        var recurring = new RecurringTransaction
        {
            Name = request.Name,
            Amount = request.Amount,
            Description = request.Description,
            Type = request.Type,
            CategoryId = request.CategoryId,
            UserId = userId,
            Frequency = request.Frequency,
            DayOfMonth = request.DayOfMonth,
            DayOfWeek = request.DayOfWeek,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            AutoGenerate = request.AutoGenerate,
            NotifyBeforeDue = request.NotifyBeforeDue,
            NotifyDaysBefore = request.NotifyDaysBefore,
            NextDueDate = nextDue,
            IsActive = true
        };

        recurring.Category = category;
        _db.Set<RecurringTransaction>().Add(recurring);
        await _db.SaveChangesAsync(ct);

        return GetRecurringTransactionsHandler.MapToDto(recurring);
    }
}

public class UpdateRecurringTransactionHandler : IRequestHandler<UpdateRecurringTransactionCommand, bool>
{
    private readonly ApplicationDbContext _db;
    public UpdateRecurringTransactionHandler(ApplicationDbContext db) => _db = db;

    public async Task<bool> Handle(UpdateRecurringTransactionCommand request, CancellationToken ct)
    {
        var userId = Guid.Parse(request.UserId);
        var item = await _db.Set<RecurringTransaction>()
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.UserId == userId, ct);
        if (item == null) return false;

        if (request.Name != null) item.Name = request.Name;
        if (request.Amount.HasValue) item.Amount = request.Amount.Value;
        if (request.Description != null) item.Description = request.Description;
        if (request.CategoryId.HasValue) item.CategoryId = request.CategoryId.Value;
        if (request.Frequency != null) item.Frequency = request.Frequency;
        if (request.DayOfMonth.HasValue) item.DayOfMonth = request.DayOfMonth.Value;
        if (request.IsActive.HasValue) item.IsActive = request.IsActive.Value;
        if (request.AutoGenerate.HasValue) item.AutoGenerate = request.AutoGenerate.Value;
        if (request.NotifyBeforeDue.HasValue) item.NotifyBeforeDue = request.NotifyBeforeDue.Value;
        if (request.NotifyDaysBefore.HasValue) item.NotifyDaysBefore = request.NotifyDaysBefore.Value;

        // Recalculate next due date if schedule changed
        if (request.Frequency != null || request.DayOfMonth.HasValue)
        {
            item.NextDueDate = BillScheduleCalculator.CalculateNextDueDate(
                item.Frequency, item.DayOfMonth, item.DayOfWeek, DateTime.UtcNow);
        }

        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

public class DeleteRecurringTransactionHandler : IRequestHandler<DeleteRecurringTransactionCommand, bool>
{
    private readonly ApplicationDbContext _db;
    public DeleteRecurringTransactionHandler(ApplicationDbContext db) => _db = db;

    public async Task<bool> Handle(DeleteRecurringTransactionCommand request, CancellationToken ct)
    {
        var userId = Guid.Parse(request.UserId);
        var item = await _db.Set<RecurringTransaction>()
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.UserId == userId, ct);
        if (item == null) return false;

        _db.Set<RecurringTransaction>().Remove(item);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

public class ToggleRecurringTransactionHandler : IRequestHandler<ToggleRecurringTransactionCommand, bool>
{
    private readonly ApplicationDbContext _db;
    public ToggleRecurringTransactionHandler(ApplicationDbContext db) => _db = db;

    public async Task<bool> Handle(ToggleRecurringTransactionCommand request, CancellationToken ct)
    {
        var userId = Guid.Parse(request.UserId);
        var item = await _db.Set<RecurringTransaction>()
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.UserId == userId, ct);
        if (item == null) return false;

        item.IsActive = !item.IsActive;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

public class GetUpcomingBillsHandler : IRequestHandler<GetUpcomingBillsQuery, List<UpcomingBillDto>>
{
    private readonly ApplicationDbContext _db;
    public GetUpcomingBillsHandler(ApplicationDbContext db) => _db = db;

    public async Task<List<UpcomingBillDto>> Handle(GetUpcomingBillsQuery request, CancellationToken ct)
    {
        var userId = Guid.Parse(request.UserId);
        var today = DateTime.UtcNow.Date;
        var cutoff = today.AddDays(request.Days);

        var items = await _db.Set<RecurringTransaction>()
            .Where(r => r.UserId == userId && r.IsActive)
            .Include(r => r.Category)
            .ToListAsync(ct);

        var upcoming = new List<UpcomingBillDto>();

        foreach (var item in items)
        {
            var nextDue = item.NextDueDate ?? BillScheduleCalculator.CalculateNextDueDate(
                item.Frequency, item.DayOfMonth, item.DayOfWeek, today);

            if (nextDue <= cutoff)
            {
                var daysUntil = (nextDue - today).Days;
                upcoming.Add(new UpcomingBillDto(
                    item.Id, item.Name, item.Amount,
                    item.Type.ToString(), item.Category?.Name,
                    nextDue, daysUntil, item.Frequency,
                    daysUntil < 0
                ));
            }
        }

        return upcoming.OrderBy(b => b.DueDate).ToList();
    }
}

public class GetBillCalendarHandler : IRequestHandler<GetBillCalendarQuery, BillCalendarDto>
{
    private readonly ApplicationDbContext _db;
    public GetBillCalendarHandler(ApplicationDbContext db) => _db = db;

    public async Task<BillCalendarDto> Handle(GetBillCalendarQuery request, CancellationToken ct)
    {
        var userId = Guid.Parse(request.UserId);
        var items = await _db.Set<RecurringTransaction>()
            .Where(r => r.UserId == userId && r.IsActive)
            .Include(r => r.Category)
            .ToListAsync(ct);

        var daysInMonth = DateTime.DaysInMonth(request.Year, request.Month);
        var today = DateTime.UtcNow.Date;
        var days = new List<CalendarDayDto>();

        decimal totalIncome = 0;
        decimal totalExpenses = 0;

        for (int day = 1; day <= daysInMonth; day++)
        {
            var date = new DateTime(request.Year, request.Month, day, 0, 0, 0, DateTimeKind.Utc);
            var dayBills = items
                .Where(r => r.DayOfMonth == day || (r.DayOfMonth > daysInMonth && day == daysInMonth))
                .Select(r =>
                {
                    if (r.Type == TransactionType.Income)
                        totalIncome += r.Amount;
                    else
                        totalExpenses += r.Amount;

                    return new CalendarBillDto(r.Id, r.Name, r.Amount, r.Type.ToString(), r.Category?.Name);
                })
                .ToList();

            days.Add(new CalendarDayDto(
                day, date,
                date.Date == today,
                false, // will be set by frontend based on user's salary day
                dayBills
            ));
        }

        return new BillCalendarDto(
            request.Month, request.Year,
            totalIncome, totalExpenses,
            totalIncome - totalExpenses,
            days
        );
    }
}

public class GetPaydayPlanHandler : IRequestHandler<GetPaydayPlanQuery, PaydayPlanResult>
{
    private readonly ApplicationDbContext _db;
    public GetPaydayPlanHandler(ApplicationDbContext db) => _db = db;

    public async Task<PaydayPlanResult> Handle(GetPaydayPlanQuery request, CancellationToken ct)
    {
        var userId = Guid.Parse(request.UserId);

        // Get recurring transactions
        var items = await _db.Set<RecurringTransaction>()
            .Where(r => r.UserId == userId && r.IsActive && r.Frequency == "Monthly")
            .Include(r => r.Category)
            .ToListAsync(ct);

        // Get monthly income from recurring income items
        var monthlyIncome = items
            .Where(r => r.Type == TransactionType.Income)
            .Sum(r => r.Amount);

        // If no recurring income, try from actual transactions this month
        if (monthlyIncome == 0)
        {
            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            monthlyIncome = await _db.Transactions
                .Where(t => t.UserId == userId && t.Type == TransactionType.Income && t.Date >= monthStart)
                .SumAsync(t => t.Amount, ct);
        }

        // Build bill items from recurring expenses
        var bills = items
            .Where(r => r.Type == TransactionType.Expense)
            .Select(r => new BillItem
            {
                Name = r.Name,
                Amount = r.Amount,
                DueDay = r.DayOfMonth,
                IsDebtPayment = r.Category?.Name?.Contains("Debt") == true ||
                               r.Category?.Name?.Contains("Loan") == true ||
                               r.Category?.Name?.Contains("Credit") == true,
                IsSavingsTransfer = r.Category?.Name?.Contains("Savings") == true ||
                                   r.Category?.Name?.Contains("Investment") == true ||
                                   r.Category?.Name?.Contains("Emergency") == true
            })
            .ToList();

        return BillScheduleCalculator.CalculatePaydayPlan(monthlyIncome, request.SalaryDay, bills);
    }
}
