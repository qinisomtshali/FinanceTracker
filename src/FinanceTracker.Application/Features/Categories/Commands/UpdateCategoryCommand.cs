using FinanceTracker.Application.Common.Interfaces;
using FinanceTracker.Application.Common.Models;
using FinanceTracker.Application.DTOs.Categories;
using FinanceTracker.Domain.Enums;
using FinanceTracker.Domain.Interfaces;
using FluentValidation;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FinanceTracker.Application.Features.Categories.Commands;

// ============================================================
// COMMAND — what we want to do
// Notice it includes the Id because we need to know WHICH category to update
// ============================================================
public record UpdateCategoryCommand(
    Guid Id,
    string Name,
    TransactionType Type,
    string? Icon
) : IRequest<Result<CategoryDto>>;

// ============================================================
// VALIDATOR — runs automatically before the handler
// ============================================================
public class UpdateCategoryValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Category ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required.")
            .MaximumLength(100).WithMessage("Category name must not exceed 100 characters.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid transaction type.");
    }
}

// ============================================================
// HANDLER — the actual business logic
// 
// KEY PATTERN: Always verify ownership before modifying.
// Just because a user sends a valid category ID doesn't mean 
// it's THEIR category. We must check UserId matches.
// This prevents IDOR (Insecure Direct Object Reference) attacks.
// ============================================================
public class UpdateCategoryHandler : IRequestHandler<UpdateCategoryCommand, Result<CategoryDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public UpdateCategoryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<CategoryDto>> Handle(
        UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(request.Id, cancellationToken);

        // Check exists
        if (category == null)
            return Result<CategoryDto>.Failure("Category not found.");

        // Check ownership — CRITICAL for security
        if (category.UserId != _currentUser.UserId)
            return Result<CategoryDto>.Failure("Category not found.");
        // ^ We return the same "not found" message instead of "not authorized"
        //   to avoid revealing that the category exists but belongs to someone else.

        // Check for duplicate name (excluding current category)
        var duplicateExists = await _unitOfWork.Categories
            .ExistsAsync(request.Name, _currentUser.UserId, cancellationToken);
        if (duplicateExists && category.Name != request.Name)
            return Result<CategoryDto>.Failure($"Category '{request.Name}' already exists.");

        // Update the entity
        category.Name = request.Name;
        category.Type = request.Type;
        category.Icon = request.Icon;

        await _unitOfWork.Categories.UpdateAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new CategoryDto(
            category.Id, category.Name, category.Type, category.Icon, category.CreatedAt);

        return Result<CategoryDto>.Success(dto);
    }
}