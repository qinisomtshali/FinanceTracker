using FinanceTracker.Application.Common.Interfaces;
using FinanceTracker.Application.Common.Models;
using FinanceTracker.Application.DTOs.Budgets;
using FinanceTracker.Domain.Interfaces;
using FluentValidation;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FinanceTracker.Application.Features.Budgets.Commands;

public record UpdateBudgetCommand(
    Guid Id,
    decimal Amount,
    int Month,
    int Year,
    Guid CategoryId
) : IRequest<Result<BudgetDto>>;

public class UpdateBudgetValidator : AbstractValidator<UpdateBudgetCommand>
{
    public UpdateBudgetValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
        RuleFor(x => x.Year).InclusiveBetween(2020, 2100);
        RuleFor(x => x.CategoryId).NotEmpty();
    }
}

public class UpdateBudgetHandler : IRequestHandler<UpdateBudgetCommand, Result<BudgetDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public UpdateBudgetHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<BudgetDto>> Handle(
        UpdateBudgetCommand request, CancellationToken cancellationToken)
    {
        var budget = await _unitOfWork.Budgets.GetByIdAsync(request.Id, cancellationToken);

        if (budget == null || budget.UserId != _currentUser.UserId)
            return Result<BudgetDto>.Failure("Budget not found.");

        var category = await _unitOfWork.Categories
            .GetByIdAsync(request.CategoryId, cancellationToken);

        if (category == null || category.UserId != _currentUser.UserId)
            return Result<BudgetDto>.Failure("Category not found.");

        budget.Amount = request.Amount;
        budget.Month = request.Month;
        budget.Year = request.Year;
        budget.CategoryId = request.CategoryId;

        await _unitOfWork.Budgets.UpdateAsync(budget, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new BudgetDto(
            budget.Id, budget.Amount, budget.Month, budget.Year,
            budget.CategoryId, category.Name, 0, budget.Amount);

        return Result<BudgetDto>.Success(dto);
    }
}