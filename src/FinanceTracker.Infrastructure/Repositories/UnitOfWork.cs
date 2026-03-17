using FinanceTracker.Domain.Interfaces;
using FinanceTracker.Infrastructure.Data;

namespace FinanceTracker.Infrastructure.Repositories;

/// <summary>
/// Unit of Work implementation — coordinates all repositories and manages
/// a single SaveChanges call for transactional consistency.
/// 
/// All repositories share the same DbContext instance (per HTTP request),
/// so when you call SaveChangesAsync, ALL pending changes across ALL
/// repositories are saved in a single database transaction.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private ICategoryRepository? _categories;
    private ITransactionRepository? _transactions;
    private IBudgetRepository? _budgets;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    // Lazy initialization — repositories are only created when first accessed
    public ICategoryRepository Categories =>
        _categories ??= new CategoryRepository(_context);

    public ITransactionRepository Transactions =>
        _transactions ??= new TransactionRepository(_context);

    public IBudgetRepository Budgets =>
        _budgets ??= new BudgetRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
