using FinanceTracker.Application.Common.Interfaces;
using FinanceTracker.Application.Common.Models;
using FinanceTracker.Application.DTOs.Reports;
using FinanceTracker.Domain.Enums;
using FinanceTracker.Domain.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FinanceTracker.Application.Features.Reports.Queries;

// ============================================================
// Category Breakdown: How much was spent per category in a month.
// Shows percentage of total spending per category — useful for
// pie charts on the frontend.
// ============================================================
public record GetCategoryBreakdownQuery(
    int Month,
    int Year
) : IRequest<Result<IReadOnlyList<CategoryBreakdownDto>>>;

public class GetCategoryBreakdownHandler
    : IRequestHandler<GetCategoryBreakdownQuery, Result<IReadOnlyList<CategoryBreakdownDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetCategoryBreakdownHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<CategoryBreakdownDto>>> Handle(
        GetCategoryBreakdownQuery request, CancellationToken cancellationToken)
    {
        var startDate = new DateTime(request.Year, request.Month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var transactions = await _unitOfWork.Transactions.GetByUserIdAsync(
            _currentUser.UserId,
            startDate: startDate,
            endDate: endDate,
            page: 1,
            pageSize: 10000,
            cancellationToken: cancellationToken);

        // Only look at expenses for the breakdown
        var expenses = transactions.Where(t => t.Type == TransactionType.Expense).ToList();
        var totalExpenses = expenses.Sum(t => t.Amount);

        var breakdown = expenses
            .GroupBy(t => new { t.CategoryId, CategoryName = t.Category?.Name ?? "Unknown" })
            .Select(g => new CategoryBreakdownDto(
                g.Key.CategoryId,
                g.Key.CategoryName,
                g.Sum(t => t.Amount),
                totalExpenses > 0
                    ? Math.Round(g.Sum(t => t.Amount) / totalExpenses * 100, 2)
                    : 0,
                g.Count()
            ))
            .OrderByDescending(x => x.Amount)
            .ToList();

        return Result<IReadOnlyList<CategoryBreakdownDto>>.Success(breakdown);
    }
}