using FinanceTracker.Domain.Common;

namespace FinanceTracker.Domain.Interfaces;

/// <summary>
/// Generic repository interface — defines the standard data operations
/// that ALL entities need.
/// 
/// WHY interfaces in Domain: This is Dependency Inversion (the D in SOLID).
/// The Domain says "I need something that can do these operations" without
/// knowing anything about Entity Framework, SQL Server, or any database.
/// The Infrastructure layer provides the actual implementation.
/// 
/// This means you could swap SQL Server for PostgreSQL, MongoDB, or even
/// a flat file — and the Domain/Application layers wouldn't change at all.
/// </summary>
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
}
