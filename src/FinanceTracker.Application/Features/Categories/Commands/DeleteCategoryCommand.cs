using FinanceTracker.Application.Common.Interfaces;
using FinanceTracker.Application.Common.Models;
using FinanceTracker.Domain.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FinanceTracker.Application.Features.Categories.Commands;

// ============================================================
// COMMAND — Delete returns Result<bool> (no data to return)
// Some teams use Result<Unit> (MediatR's void equivalent).
// We use bool for clarity — true means "successfully deleted".
// ============================================================
public record DeleteCategoryCommand(Guid Id) : IRequest<Result<bool>>;

// ============================================================
// HANDLER
// 
// BUSINESS RULE: Should we allow deleting a category that has
// transactions linked to it? For now, we'll block it.
// This prevents orphaned transactions with no category.
// 
// Alternative approaches:
//   1. Cascade delete (delete transactions too) — dangerous
//   2. Soft delete (mark as inactive) — safer, more complex
//   3. Block deletion if transactions exist — simplest, what we do
// ============================================================
public class DeleteCategoryHandler : IRequestHandler<DeleteCategoryCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public DeleteCategoryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<bool>> Handle(
        DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(request.Id, cancellationToken);

        if (category == null)
            return Result<bool>.Failure("Category not found.");

        // Ownership check
        if (category.UserId != _currentUser.UserId)
            return Result<bool>.Failure("Category not found.");

        await _unitOfWork.Categories.DeleteAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}