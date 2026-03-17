using FinanceTracker.Application.Common.Interfaces;
using FinanceTracker.Application.Common.Models;
using FinanceTracker.Application.DTOs.Budgets;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Interfaces;
using FluentValidation;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FinanceTracker.Application.Features.Budgets.Commands;

public record CreateBudgetCommand(
    decimal Amount,
    int Month,
    int Year,
    Guid CategoryId
) : IRequest<Result<BudgetDto>>;

public class CreateBudgetValidator : AbstractValidator<CreateBudgetCommand>
{
    public CreateBudgetValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Budget amount must be greater than zero.");

        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12).WithMessage("Month must be between 1 and 12.");

        RuleFor(x => x.Year)
            .InclusiveBetween(2020, 2100).WithMessage("Year must be between 2020 and 2100.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category is required.");
    }
}

public class CreateBudgetHandler : IRequestHandler<CreateBudgetCommand, Result<BudgetDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CreateBudgetHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<BudgetDto>> Handle(
        CreateBudgetCommand request, CancellationToken cancellationToken)
    {
        // Verify category ownership
        var category = await _unitOfWork.Categories
            .GetByIdAsync(request.CategoryId, cancellationToken);

        if (category == null || category.UserId != _currentUser.UserId)
            return Result<BudgetDto>.Failure("Category not found.");

        // Check if budget already exists for this category/month/year
        var existing = await _unitOfWork.Budgets.GetByUserAndCategoryAsync(
            _currentUser.UserId, request.CategoryId, request.Month, request.Year,
            cancellationToken);

        if (existing != null)
            return Result<BudgetDto>.Failure(
                $"A budget for {category.Name} in {request.Month}/{request.Year} already exists.");

        var budget = new Budget
        {
            Id = Guid.NewGuid(),
            Amount = request.Amount,
            Month = request.Month,
            Year = request.Year,
            CategoryId = request.CategoryId,
            UserId = _currentUser.UserId
        };

        await _unitOfWork.Budgets.AddAsync(budget, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new BudgetDto(
            budget.Id, budget.Amount, budget.Month, budget.Year,
            budget.CategoryId, category.Name,
            0,                // SpentAmount — nothing spent yet
            budget.Amount     // RemainingAmount — full budget available
        );

        return Result<BudgetDto>.Success(dto);
    }
}