using FinanceTracker.Application.Common.Interfaces;
using FinanceTracker.Application.Common.Models;
using FinanceTracker.Application.DTOs.Reports;
using FinanceTracker.Domain.Enums;
using FinanceTracker.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FinanceTracker.Application.Features.Reports.Queries;

// ============================================================
// Budget vs Actual: Compare what you planned to spend vs what
// you actually spent. This is the "am I on track?" report.
// ============================================================
public record GetBudgetVsActualQuery(
    int Month,
    int Year
) : IRequest<Result<IReadOnlyList<BudgetVsActualDto>>>;

public class GetBudgetVsActualHandler
    : IRequestHandler<GetBudgetVsActualQuery, Result<IReadOnlyList<BudgetVsActualDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetBudgetVsActualHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<BudgetVsActualDto>>> Handle(
        GetBudgetVsActualQuery request, CancellationToken cancellationToken)
    {
        var budgets = await _unitOfWork.Budgets.GetByUserIdAsync(
            _currentUser.UserId, request.Month, request.Year, cancellationToken);

        var startDate = new DateTime(request.Year, request.Month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var dtos = new List<BudgetVsActualDto>();

        foreach (var budget in budgets)
        {
            var transactions = await _unitOfWork.Transactions.GetByUserIdAsync(
                _currentUser.UserId,
                startDate: startDate,
                endDate: endDate,
                categoryId: budget.CategoryId,
                page: 1,
                pageSize: int.MaxValue,
                cancellationToken: cancellationToken);

            var actualAmount = transactions
                .Where(t => t.Type == TransactionType.Expense)
                .Sum(t => t.Amount);

            dtos.Add(new BudgetVsActualDto(
                budget.CategoryId,
                budget.Category?.Name ?? "Unknown",
                budget.Amount,
                actualAmount,
                budget.Amount - actualAmount,
                budget.Amount > 0
                    ? Math.Round(actualAmount / budget.Amount * 100, 2)
                    : 0
            ));
        }

        return Result<IReadOnlyList<BudgetVsActualDto>>.Success(dtos);
    }
}