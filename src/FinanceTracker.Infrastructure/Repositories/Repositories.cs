using FinanceTracker.Domain.Common;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Interfaces;
using FinanceTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Infrastructure.Repositories;

/// <summary>
/// Generic repository implementation using EF Core.
/// 
/// This is the IMPLEMENTATION of the interface defined in Domain.
/// Domain said "I need something that can Get, Add, Update, Delete."
/// Infrastructure says "Here it is, using Entity Framework Core."
/// 
/// The Application/Domain layers never see this class directly —
/// they only interact through the IRepository<T> interface.
/// </summary>
public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }

    public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
        return entity;
    }

    public Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        _context.Entry(entity).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Remove(entity);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Transaction repository with custom query methods for filtering and pagination.
/// </summary>
public class TransactionRepository : Repository<Transaction>, ITransactionRepository
{
    public TransactionRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Transaction>> GetByUserIdAsync(
        Guid userId, DateTime? startDate = null, DateTime? endDate = null,
        Guid? categoryId = null, int page = 1, int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(t => t.Category) // Eager load category for the DTO mapping
            .Where(t => t.UserId == userId);

        if (startDate.HasValue)
            query = query.Where(t => t.Date >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(t => t.Date <= endDate.Value);
        if (categoryId.HasValue)
            query = query.Where(t => t.CategoryId == categoryId.Value);

        return await query
            .OrderByDescending(t => t.Date)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountByUserIdAsync(
        Guid userId, DateTime? startDate = null, DateTime? endDate = null,
        Guid? categoryId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(t => t.UserId == userId);

        if (startDate.HasValue)
            query = query.Where(t => t.Date >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(t => t.Date <= endDate.Value);
        if (categoryId.HasValue)
            query = query.Where(t => t.CategoryId == categoryId.Value);

        return await query.CountAsync(cancellationToken);
    }
}

public class CategoryRepository : Repository<Category>, ICategoryRepository
{
    public CategoryRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Category>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(string name, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(c => c.Name == name && c.UserId == userId, cancellationToken);
    }
}

public class BudgetRepository : Repository<Budget>, IBudgetRepository
{
    public BudgetRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Budget>> GetByUserIdAsync(
        Guid userId, int? month = null, int? year = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(b => b.Category)
            .Where(b => b.UserId == userId);

        if (month.HasValue)
            query = query.Where(b => b.Month == month.Value);
        if (year.HasValue)
            query = query.Where(b => b.Year == year.Value);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<Budget?> GetByUserAndCategoryAsync(
        Guid userId, Guid categoryId, int month, int year, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(
            b => b.UserId == userId && b.CategoryId == categoryId && b.Month == month && b.Year == year,
            cancellationToken);
    }
}
