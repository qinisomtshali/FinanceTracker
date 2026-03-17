using FinanceTracker.Application.Common.Interfaces;
using FinanceTracker.Application.Common.Models;
using FinanceTracker.Application.DTOs.Transactions;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Enums;
using FinanceTracker.Domain.Interfaces;
using FluentValidation;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FinanceTracker.Application.Features.Transactions.Commands;

// ============================================================
// COMMAND
// ============================================================
public record CreateTransactionCommand(
    decimal Amount,
    string? Description,
    DateTime Date,
    TransactionType Type,
    Guid CategoryId
) : IRequest<Result<TransactionDto>>;

// ============================================================
// VALIDATOR
// 
// Notice we validate business rules here:
//   - Amount must be positive (we said amounts are always positive)
//   - Date can't be in the future (you can't spend money tomorrow)
//   - CategoryId must be provided
// ============================================================
public class CreateTransactionValidator : AbstractValidator<CreateTransactionCommand>
{
    public CreateTransactionValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Date is required.")
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
            .WithMessage("Date cannot be in the future.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid transaction type.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category is required.");
    }
}

// ============================================================
// HANDLER
// 
// NEW CONCEPT: Cross-entity validation.
// We need to verify the CategoryId actually belongs to this user.
// A malicious user could send someone else's category ID.
// ============================================================
public class CreateTransactionHandler
    : IRequestHandler<CreateTransactionCommand, Result<TransactionDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CreateTransactionHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<TransactionDto>> Handle(
        CreateTransactionCommand request, CancellationToken cancellationToken)
    {
        // Verify the category exists AND belongs to this user
        var category = await _unitOfWork.Categories
            .GetByIdAsync(request.CategoryId, cancellationToken);

        if (category == null || category.UserId != _currentUser.UserId)
            return Result<TransactionDto>.Failure("Category not found.");

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            Amount = request.Amount,
            Description = request.Description,
            Date = request.Date,
            Type = request.Type,
            CategoryId = request.CategoryId,
            UserId = _currentUser.UserId
        };

        await _unitOfWork.Transactions.AddAsync(transaction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new TransactionDto(
            transaction.Id,
            transaction.Amount,
            transaction.Description,
            transaction.Date,
            transaction.Type,
            transaction.CategoryId,
            category.Name,  // We already loaded the category, so use it
            transaction.CreatedAt
        );

        return Result<TransactionDto>.Success(dto);
    }
}