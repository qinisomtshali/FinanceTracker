using FinanceTracker.Domain.Entities;

namespace FinanceTracker.Domain.Interfaces;

/// <summary>
/// Transaction-specific repository operations beyond basic CRUD.
/// 
/// WHY a separate interface: Generic CRUD isn't enough. Transactions need
/// filtering by date range, user, category, pagination, etc. These are
/// domain-specific query needs that belong in a dedicated interface.
/// </summary>
public interface ITransactionRepository : IRepository<Transaction>
{
    Task<IReadOnlyList<Transaction>> GetByUserIdAsync(
        Guid userId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        Guid? categoryId = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<int> GetCountByUserIdAsync(
        Guid userId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        Guid? categoryId = null,
        CancellationToken cancellationToken = default);
}

public interface ICategoryRepository : IRepository<Category>
{
    Task<IReadOnlyList<Category>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string name, Guid userId, CancellationToken cancellationToken = default);
}

public interface IBudgetRepository : IRepository<Budget>
{
    Task<IReadOnlyList<Budget>> GetByUserIdAsync(Guid userId, int? month = null, int? year = null, CancellationToken cancellationToken = default);
    Task<Budget?> GetByUserAndCategoryAsync(Guid userId, Guid categoryId, int month, int year, CancellationToken cancellationToken = default);
}
