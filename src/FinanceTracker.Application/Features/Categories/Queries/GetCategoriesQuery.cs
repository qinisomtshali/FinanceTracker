using FinanceTracker.Application.Common.Interfaces;
using FinanceTracker.Application.Common.Models;
using FinanceTracker.Application.DTOs.Categories;
using FinanceTracker.Domain.Interfaces;
using MediatR;

namespace FinanceTracker.Application.Features.Categories.Queries;

/// <summary>
/// A QUERY — it only reads data, never modifies state.
/// In CQRS, separating reads from writes lets you optimize each independently.
/// </summary>
public record GetCategoriesQuery : IRequest<Result<IReadOnlyList<CategoryDto>>>;

public class GetCategoriesHandler : IRequestHandler<GetCategoriesQuery, Result<IReadOnlyList<CategoryDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetCategoriesHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<CategoryDto>>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = await _unitOfWork.Categories.GetByUserIdAsync(_currentUser.UserId, cancellationToken);

        var dtos = categories.Select(c => new CategoryDto(
            c.Id,
            c.Name,
            c.Type,
            c.Icon,
            c.CreatedAt
        )).ToList();

        return Result<IReadOnlyList<CategoryDto>>.Success(dtos);
    }
}
