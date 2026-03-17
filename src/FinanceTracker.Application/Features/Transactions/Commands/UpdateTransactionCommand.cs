using FinanceTracker.Application.Common.Interfaces;
using FinanceTracker.Application.Common.Models;
using FinanceTracker.Application.DTOs.Transactions;
using FinanceTracker.Domain.Enums;
using FinanceTracker.Domain.Interfaces;
using FluentValidation;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FinanceTracker.Application.Features.Transactions.Commands;

public record UpdateTransactionCommand(
    Guid Id,
    decimal Amount,
    string? Description,
    DateTime Date,
    TransactionType Type,
    Guid CategoryId
) : IRequest<Result<TransactionDto>>;

public class UpdateTransactionValidator : AbstractValidator<UpdateTransactionCommand>
{
    public UpdateTransactionValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.CategoryId).NotEmpty();
    }
}

public class UpdateTransactionHandler
    : IRequestHandler<UpdateTransactionCommand, Result<TransactionDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public UpdateTransactionHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<TransactionDto>> Handle(
        UpdateTransactionCommand request, CancellationToken cancellationToken)
    {
        var transaction = await _unitOfWork.Transactions
            .GetByIdAsync(request.Id, cancellationToken);

        if (transaction == null || transaction.UserId != _currentUser.UserId)
            return Result<TransactionDto>.Failure("Transaction not found.");

        // Verify new category belongs to user
        var category = await _unitOfWork.Categories
            .GetByIdAsync(request.CategoryId, cancellationToken);

        if (category == null || category.UserId != _currentUser.UserId)
            return Result<TransactionDto>.Failure("Category not found.");

        // Update fields
        transaction.Amount = request.Amount;
        transaction.Description = request.Description;
        transaction.Date = request.Date;
        transaction.Type = request.Type;
        transaction.CategoryId = request.CategoryId;

        await _unitOfWork.Transactions.UpdateAsync(transaction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new TransactionDto(
            transaction.Id, transaction.Amount, transaction.Description,
            transaction.Date, transaction.Type, transaction.CategoryId,
            category.Name, transaction.CreatedAt);

        return Result<TransactionDto>.Success(dto);
    }
}