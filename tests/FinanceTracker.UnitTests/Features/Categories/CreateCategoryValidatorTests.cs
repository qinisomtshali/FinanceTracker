using FinanceTracker.Application.Features.Categories.Commands;
using FinanceTracker.Domain.Enums;
using FluentAssertions;
using System.Threading.Tasks;
using Xunit;

namespace FinanceTracker.UnitTests.Features.Categories;

/// <summary>
/// Tests for the FluentValidation validator.
/// 
/// WHY TEST VALIDATORS SEPARATELY: Validators are pure logic with no
/// dependencies — they're the easiest things to test. And validation
/// bugs are some of the most common (allowing empty names, negative
/// amounts, etc). These tests are your safety net.
/// </summary>
public class CreateCategoryValidatorTests
{
    private readonly CreateCategoryValidator _validator = new();

    [Fact]
    public async Task Validate_ValidCommand_PassesValidation()
    {
        // Arrange
        var command = new CreateCategoryCommand("Groceries", TransactionType.Expense, "cart");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_EmptyName_FailsValidation()
    {
        var command = new CreateCategoryCommand("", TransactionType.Expense, "cart");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validate_NameTooLong_FailsValidation()
    {
        var longName = new string('a', 101); // 101 characters
        var command = new CreateCategoryCommand(longName, TransactionType.Expense, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validate_InvalidType_FailsValidation()
    {
        var command = new CreateCategoryCommand("Test", (TransactionType)99, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Type");
    }
}