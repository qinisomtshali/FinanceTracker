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
// Monthly Summary: Total income vs total expenses for a month.
// This is the kind of endpoint that makes an API genuinely useful
// — the frontend doesn't have to fetch all transactions and
// calculate totals itself. The server does the heavy lifting.
// ============================================================
public record GetMonthlySummaryQuery(
    int Month,
    int Year
) : IRequest<Result<MonthlySummaryDto>>;

public class GetMonthlySummaryHandler
    : IRequestHandler<GetMonthlySummaryQuery, Result<MonthlySummaryDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetMonthlySummaryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<MonthlySummaryDto>> Handle(
        GetMonthlySummaryQuery request, CancellationToken cancellationToken)
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

        var totalIncome = transactions
            .Where(t => t.Type == TransactionType.Income)
            .Sum(t => t.Amount);

        var totalExpenses = transactions
            .Where(t => t.Type == TransactionType.Expense)
            .Sum(t => t.Amount);

        var dto = new MonthlySummaryDto(
            request.Month,
            request.Year,
            totalIncome,
            totalExpenses,
            totalIncome - totalExpenses
        );

        return Result<MonthlySummaryDto>.Success(dto);
    }
}