using FinanceTracker.Application.Common.Interfaces;
using FinanceTracker.Application.Common.Models;
using FinanceTracker.Application.DTOs.Transactions;
using FinanceTracker.Domain.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FinanceTracker.Application.Features.Transactions.Queries;

// ============================================================
// QUERY with filtering and pagination parameters
// 
// WHY PAGINATION: Without it, a user with 10,000 transactions
// would get ALL of them in one response. That's slow for the API,
// slow for the network, and slow for the frontend to render.
// 
// Instead, we return 20 at a time with metadata (total count,
// page number, has next page) so the frontend can paginate.
//
// FILTERING: Optional parameters let the frontend request
// "show me only Groceries expenses from March 2026".
// ============================================================
public record GetTransactionsQuery(
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    Guid? CategoryId = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<Result<PaginatedList<TransactionDto>>>;

public class GetTransactionsHandler
    : IRequestHandler<GetTransactionsQuery, Result<PaginatedList<TransactionDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetTransactionsHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<PaginatedList<TransactionDto>>> Handle(
        GetTransactionsQuery request, CancellationToken cancellationToken)
    {
        // Get the paginated data
        var transactions = await _unitOfWork.Transactions.GetByUserIdAsync(
            _currentUser.UserId,
            request.StartDate,
            request.EndDate,
            request.CategoryId,
            request.Page,
            request.PageSize,
            cancellationToken);

        // Get total count for pagination metadata
        var totalCount = await _unitOfWork.Transactions.GetCountByUserIdAsync(
            _currentUser.UserId,
            request.StartDate,
            request.EndDate,
            request.CategoryId,
            cancellationToken);

        // Map to DTOs
        var dtos = transactions.Select(t => new TransactionDto(
            t.Id,
            t.Amount,
            t.Description,
            t.Date,
            t.Type,
            t.CategoryId,
            t.Category?.Name ?? "Unknown",
            t.CreatedAt
        )).ToList();

        var paginatedList = new PaginatedList<TransactionDto>(
            dtos, totalCount, request.Page, request.PageSize);

        return Result<PaginatedList<TransactionDto>>.Success(paginatedList);
    }
}