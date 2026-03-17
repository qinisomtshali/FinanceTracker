using FinanceTracker.Domain.Enums;

namespace FinanceTracker.Application.DTOs.Categories;

public record CategoryDto(
    Guid Id,
    string Name,
    TransactionType Type,
    string? Icon,
    DateTime CreatedAt
);

public record CreateCategoryDto(
    string Name,
    TransactionType Type,
    string? Icon
);

public record UpdateCategoryDto(
    string Name,
    TransactionType Type,
    string? Icon
);
