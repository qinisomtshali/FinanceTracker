using FinanceTracker.Application.Common.Interfaces;
using FinanceTracker.Application.Features.Categories.Commands;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Enums;
using FinanceTracker.Domain.Interfaces;
using FluentAssertions;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FinanceTracker.UnitTests.Features.Categories;

/// <summary>
/// Unit tests for CreateCategoryHandler.
/// 
/// ANATOMY OF A UNIT TEST (Arrange-Act-Assert pattern):
///   Arrange — set up the mocks and test data
///   Act     — call the method being tested
///   Assert  — verify the result is what you expected
/// 
/// NAMING CONVENTION: MethodName_Scenario_ExpectedResult
///   e.g., Handle_ValidCommand_ReturnsSuccessWithCategory
///   This reads like a sentence and tells you exactly what failed.
/// 
/// WHY MOQ: We don't want to hit a real database in unit tests.
///   Moq creates fake implementations of interfaces that we control.
///   We tell the mock "when someone calls ExistsAsync, return false"
///   and the handler thinks it's talking to a real repository.
/// </summary>
public class CreateCategoryHandlerTests
{
    // These are the mocked dependencies — reused across tests
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICategoryRepository> _categoryRepoMock;
    private readonly Mock<ICurrentUserService> _currentUserMock;
    private readonly CreateCategoryHandler _handler;
    private readonly Guid _testUserId = Guid.NewGuid();

    /// <summary>
    /// Constructor runs before EACH test — gives every test a clean slate.
    /// This is xUnit's way of doing test setup (no [SetUp] attribute like NUnit).
    /// </summary>
    public CreateCategoryHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _categoryRepoMock = new Mock<ICategoryRepository>();
        _currentUserMock = new Mock<ICurrentUserService>();

        // Wire up: UnitOfWork.Categories returns our mocked category repo
        _unitOfWorkMock.Setup(u => u.Categories).Returns(_categoryRepoMock.Object);

        // Current user always returns our test user ID
        _currentUserMock.Setup(u => u.UserId).Returns(_testUserId);

        // Create the handler with mocked dependencies
        _handler = new CreateCategoryHandler(_unitOfWorkMock.Object, _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessWithCategory()
    {
        // Arrange
        var command = new CreateCategoryCommand("Groceries", TransactionType.Expense, "cart");

        _categoryRepoMock
            .Setup(r => r.ExistsAsync("Groceries", _testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // No duplicate exists

        _categoryRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category c, CancellationToken _) => c); // Return the same entity

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1); // 1 row affected

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be("Groceries");
        result.Data.Type.Should().Be(TransactionType.Expense);
        result.Data.Icon.Should().Be("cart");

        // Verify the repository was actually called (not just that the result is correct)
        _categoryRepoMock.Verify(
            r => r.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateName_ReturnsFailure()
    {
        // Arrange
        var command = new CreateCategoryCommand("Groceries", TransactionType.Expense, "cart");

        _categoryRepoMock
            .Setup(r => r.ExistsAsync("Groceries", _testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // Duplicate EXISTS

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("already exists"));

        // Verify AddAsync was NEVER called — we short-circuited before saving
        _categoryRepoMock.Verify(
            r => r.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ValidCommand_SetCorrectUserId()
    {
        // Arrange — this test verifies the handler uses the CURRENT USER's ID
        var command = new CreateCategoryCommand("Transport", TransactionType.Expense, "car");

        _categoryRepoMock
            .Setup(r => r.ExistsAsync(It.IsAny<string>(), _testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Category? capturedCategory = null;
        _categoryRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
            .Callback<Category, CancellationToken>((c, _) => capturedCategory = c)
            .ReturnsAsync((Category c, CancellationToken _) => c);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert — the entity saved to the DB must have the correct UserId
        capturedCategory.Should().NotBeNull();
        capturedCategory!.UserId.Should().Be(_testUserId);
    }
}