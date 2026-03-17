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

public class DeleteCategoryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICategoryRepository> _categoryRepoMock;
    private readonly Mock<ICurrentUserService> _currentUserMock;
    private readonly DeleteCategoryHandler _handler;
    private readonly Guid _testUserId = Guid.NewGuid();

    public DeleteCategoryHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _categoryRepoMock = new Mock<ICategoryRepository>();
        _currentUserMock = new Mock<ICurrentUserService>();

        _unitOfWorkMock.Setup(u => u.Categories).Returns(_categoryRepoMock.Object);
        _currentUserMock.Setup(u => u.UserId).Returns(_testUserId);

        _handler = new DeleteCategoryHandler(_unitOfWorkMock.Object, _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingOwnedCategory_ReturnsSuccess()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = new Category
        {
            Id = categoryId,
            Name = "Test",
            Type = TransactionType.Expense,
            UserId = _testUserId // Owned by current user
        };

        _categoryRepoMock
            .Setup(r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(new DeleteCategoryCommand(categoryId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _categoryRepoMock.Verify(
            r => r.DeleteAsync(category, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistentCategory_ReturnsFailure()
    {
        // Arrange
        _categoryRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null); // Not found

        // Act
        var result = await _handler.Handle(
            new DeleteCategoryCommand(Guid.NewGuid()), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Category not found.");
    }

    [Fact]
    public async Task Handle_CategoryOwnedByDifferentUser_ReturnsFailure()
    {
        // Arrange — category exists but belongs to someone else
        var categoryId = Guid.NewGuid();
        var category = new Category
        {
            Id = categoryId,
            Name = "Not Mine",
            Type = TransactionType.Expense,
            UserId = Guid.NewGuid() // Different user!
        };

        _categoryRepoMock
            .Setup(r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        // Act
        var result = await _handler.Handle(
            new DeleteCategoryCommand(categoryId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Category not found.");

        // Verify delete was NEVER called — security check blocked it
        _categoryRepoMock.Verify(
            r => r.DeleteAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}