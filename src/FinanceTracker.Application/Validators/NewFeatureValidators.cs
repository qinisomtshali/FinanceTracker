using FluentValidation;
using FinanceTracker.Application.Features.Stocks;
using FinanceTracker.Application.Features.Crypto;
using FinanceTracker.Application.Features.Tax;
using FinanceTracker.Application.Features.Invoices;

namespace FinanceTracker.Application.Validators;

// ─── Stock Validators ───────────────────────────────────────────

public class AddToWatchlistValidator : AbstractValidator<AddToWatchlistCommand>
{
    public AddToWatchlistValidator()
    {
        RuleFor(x => x.Symbol)
            .NotEmpty().WithMessage("Stock symbol is required")
            .MaximumLength(20).WithMessage("Symbol cannot exceed 20 characters");
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Stock name is required")
            .MaximumLength(200);
        RuleFor(x => x.AlertPriceAbove)
            .GreaterThan(0).When(x => x.AlertPriceAbove.HasValue)
            .WithMessage("Alert price must be positive");
        RuleFor(x => x.AlertPriceBelow)
            .GreaterThan(0).When(x => x.AlertPriceBelow.HasValue)
            .WithMessage("Alert price must be positive");
    }
}

// ─── Crypto Validators ──────────────────────────────────────────

public class AddCryptoToWatchlistValidator : AbstractValidator<AddCryptoToWatchlistCommand>
{
    public AddCryptoToWatchlistValidator()
    {
        RuleFor(x => x.CoinId)
            .NotEmpty().WithMessage("Coin ID is required")
            .MaximumLength(100);
        RuleFor(x => x.Symbol)
            .NotEmpty().WithMessage("Coin symbol is required")
            .MaximumLength(20);
        RuleFor(x => x.Name)
            .NotEmpty().MaximumLength(200);
        RuleFor(x => x.HoldingQuantity)
            .GreaterThan(0).When(x => x.HoldingQuantity.HasValue);
        RuleFor(x => x.AverageBuyPrice)
            .GreaterThan(0).When(x => x.AverageBuyPrice.HasValue);
    }
}

// ─── Tax Validators ─────────────────────────────────────────────

public class CalculateTaxValidator : AbstractValidator<CalculateTaxQuery>
{
    public CalculateTaxValidator()
    {
        RuleFor(x => x.GrossIncome)
            .GreaterThan(0).WithMessage("Gross income must be greater than zero")
            .LessThanOrEqualTo(100_000_000m).WithMessage("Please enter a realistic income value");
        RuleFor(x => x.Age)
            .InclusiveBetween(18, 120).WithMessage("Age must be between 18 and 120");
        RuleFor(x => x.RetirementContributions)
            .GreaterThanOrEqualTo(0).WithMessage("Retirement contributions cannot be negative");
        RuleFor(x => x.MedicalAidMembers)
            .InclusiveBetween(0, 20).WithMessage("Medical aid members must be between 0 and 20");
    }
}

// ─── Invoice Validators ─────────────────────────────────────────

public class CreateInvoiceValidator : AbstractValidator<CreateInvoiceCommand>
{
    public CreateInvoiceValidator()
    {
        RuleFor(x => x.FromName)
            .NotEmpty().WithMessage("Sender name is required")
            .MaximumLength(200);
        RuleFor(x => x.ToName)
            .NotEmpty().WithMessage("Recipient name is required")
            .MaximumLength(200);
        RuleFor(x => x.DueDate)
            .GreaterThanOrEqualTo(DateTime.UtcNow.Date)
            .WithMessage("Due date must be today or in the future");
        RuleFor(x => x.VatRate)
            .InclusiveBetween(0, 100)
            .WithMessage("VAT rate must be between 0 and 100");
        RuleFor(x => x.DiscountPercentage)
            .InclusiveBetween(0, 100)
            .When(x => x.DiscountPercentage.HasValue)
            .WithMessage("Discount must be between 0 and 100");
        RuleFor(x => x.Currency)
            .MaximumLength(10);

        RuleFor(x => x.LineItems)
            .NotEmpty().WithMessage("At least one line item is required");

        RuleForEach(x => x.LineItems).SetValidator(new CreateLineItemValidator());
    }
}

public class CreateLineItemValidator : AbstractValidator<CreateLineItemDto>
{
    public CreateLineItemValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Line item description is required")
            .MaximumLength(500);
        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero");
        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Unit price cannot be negative");
    }
}
