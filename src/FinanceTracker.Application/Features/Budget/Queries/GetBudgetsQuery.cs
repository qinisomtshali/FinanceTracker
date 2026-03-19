using FinanceTracker.Application.Common.Interfaces;
using FinanceTracker.Application.Common.Models;
using FinanceTracker.Application.DTOs.Budgets;
using FinanceTracker.Domain.Enums;
using FinanceTracker.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FinanceTracker.Application.Features.Budgets.Queries;

// ============================================================
// This query does something interesting: it CALCULATES spent
// amounts by cross-referencing Transactions for each budget.
// This is where read-side logic gets richer than simple CRUD.
// ============================================================
public record GetBudgetsQuery(
    int? Month = null,
    int? Year = null
) : IRequest<Result<IReadOnlyList<BudgetDto>>>;

public class GetBudgetsHandler
    : IRequestHandler<GetBudgetsQuery, Result<IReadOnlyList<BudgetDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetBudgetsHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<BudgetDto>>> Handle(
        GetBudgetsQuery request, CancellationToken cancellationToken)
    {
        var budgets = await _unitOfWork.Budgets.GetByUserIdAsync(
            _currentUser.UserId, request.Month, request.Year, cancellationToken);

        var dtos = new List<BudgetDto>();

        foreach (var budget in budgets)
        {
            // Calculate how much was actually spent in this category for this month
            var startDate = new DateTime(budget.Year, budget.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var transactions = await _unitOfWork.Transactions.GetByUserIdAsync(
                _currentUser.UserId,
                startDate: startDate,
                endDate: endDate,
                categoryId: budget.CategoryId,
                page: 1,
                pageSize: 10000, // Get all for this month/category
                cancellationToken: cancellationToken);

            var spentAmount = transactions
                .Where(t => t.Type == TransactionType.Expense)
                .Sum(t => t.Amount);

            dtos.Add(new BudgetDto(
                budget.Id,
                budget.Amount,
                budget.Month,
                budget.Year,
                budget.CategoryId,
                budget.Category?.Name ?? "Unknown",
                spentAmount,
                budget.Amount - spentAmount
            ));
        }

        return Result<IReadOnlyList<BudgetDto>>.Success(dtos);
    }
}