using FinanceTracker.Application.Common.Interfaces;
using FinanceTracker.Application.Common.Models;
using FinanceTracker.Application.DTOs.Categories;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Interfaces;
using MediatR;

namespace FinanceTracker.Application.Features.Categories.Commands;

/// <summary>
/// Handler for CreateCategoryCommand — contains the actual business logic.
/// 
/// NOTICE the dependencies injected via constructor:
///   - IUnitOfWork: for data access (defined in Domain, implemented in Infrastructure)
///   - ICurrentUserService: to get the logged-in user's ID
/// 
/// The handler doesn't know about HTTP, controllers, or databases.
/// It just orchestrates domain logic and returns a Result.
/// </summary>
public class CreateCategoryHandler : IRequestHandler<CreateCategoryCommand, Result<CategoryDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CreateCategoryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<CategoryDto>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        // Business rule: no duplicate category names per user
        var exists = await _unitOfWork.Categories.ExistsAsync(request.Name, _currentUser.UserId, cancellationToken);
        if (exists)
            return Result<CategoryDto>.Failure($"Category '{request.Name}' already exists.");

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Type = request.Type,
            Icon = request.Icon,
            UserId = _currentUser.UserId
        };

        await _unitOfWork.Categories.AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Map entity to DTO before returning — never expose the raw entity
        var dto = new CategoryDto(
            category.Id,
            category.Name,
            category.Type,
            category.Icon,
            category.CreatedAt
        );

        return Result<CategoryDto>.Success(dto);
    }
}
