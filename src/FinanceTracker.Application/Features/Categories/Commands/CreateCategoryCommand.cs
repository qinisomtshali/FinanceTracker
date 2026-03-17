using FinanceTracker.Application.Common.Models;
using FinanceTracker.Application.DTOs.Categories;
using FinanceTracker.Domain.Enums;
using MediatR;

namespace FinanceTracker.Application.Features.Categories.Commands;

/// <summary>
/// CQRS — Command Query Responsibility Segregation.
/// 
/// This is a COMMAND (it changes state — creates a category).
/// Commands return Result<T> to indicate success or failure.
/// 
/// HOW IT WORKS:
///   1. Controller receives HTTP POST with CreateCategoryDto
///   2. Controller creates this Command and sends it via MediatR
///   3. MediatR finds the matching Handler (below) and executes it
///   4. Handler uses repositories to create the category
///   5. Result flows back through MediatR → Controller → HTTP Response
/// 
/// WHY MEDIATR: It decouples the "what" (this command) from the "how" (the handler).
/// Controllers become thin — they just translate HTTP to commands/queries.
/// All business logic lives in handlers, which are easy to test.
/// </summary>
public record CreateCategoryCommand(
    string Name,
    TransactionType Type,
    string? Icon
) : IRequest<Result<CategoryDto>>;
