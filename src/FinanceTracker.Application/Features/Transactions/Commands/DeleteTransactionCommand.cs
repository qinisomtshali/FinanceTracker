using FinanceTracker.Application.Common.Interfaces;
using FinanceTracker.Application.Common.Models;
using FinanceTracker.Domain.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FinanceTracker.Application.Features.Transactions.Commands;

public record DeleteTransactionCommand(Guid Id) : IRequest<Result<bool>>;

public class DeleteTransactionHandler
    : IRequestHandler<DeleteTransactionCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public DeleteTransactionHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<bool>> Handle(
        DeleteTransactionCommand request, CancellationToken cancellationToken)
    {
        var transaction = await _unitOfWork.Transactions
            .GetByIdAsync(request.Id, cancellationToken);

        if (transaction == null || transaction.UserId != _currentUser.UserId)
            return Result<bool>.Failure("Transaction not found.");

        await _unitOfWork.Transactions.DeleteAsync(transaction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}