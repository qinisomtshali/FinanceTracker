using FluentValidation;

namespace FinanceTracker.Application.Features.Categories.Commands;

/// <summary>
/// Validator for CreateCategoryCommand.
/// 
/// This is automatically picked up by the ValidationBehavior we registered
/// in the MediatR pipeline. When someone sends a CreateCategoryCommand,
/// this validator runs BEFORE the handler. If validation fails,
/// the handler never executes.
/// 
/// WHY FLUENTVALIDATION over Data Annotations:
///   1. Testable — validators are plain classes you can unit test
///   2. Separation — validation logic isn't scattered across DTOs
///   3. Powerful — complex rules, conditional validation, async validation
///   4. Readable — fluent API reads like English
/// </summary>
public class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required.")
            .MaximumLength(100).WithMessage("Category name must not exceed 100 characters.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid transaction type. Must be Expense (0) or Income (1).");
    }
}
