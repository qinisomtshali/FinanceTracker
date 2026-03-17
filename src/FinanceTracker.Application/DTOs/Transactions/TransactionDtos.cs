using FinanceTracker.Domain.Enums;

namespace FinanceTracker.Application.DTOs.Transactions;

public record TransactionDto(
    Guid Id,
    decimal Amount,
    string? Description,
    DateTime Date,
    TransactionType Type,
    Guid CategoryId,
    string CategoryName,
    DateTime CreatedAt
);

public record CreateTransactionDto(
    decimal Amount,
    string? Description,
    DateTime Date,
    TransactionType Type,
    Guid CategoryId
);

public record UpdateTransactionDto(
    decimal Amount,
    string? Description,
    DateTime Date,
    TransactionType Type,
    Guid CategoryId
);
