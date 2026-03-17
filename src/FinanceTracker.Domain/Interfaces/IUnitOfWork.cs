namespace FinanceTracker.Domain.Interfaces;

/// <summary>
/// Unit of Work pattern — coordinates saving changes across multiple repositories.
/// 
/// WHY: Without this, each repository calls SaveChanges() independently.
/// If you add a Category and a Transaction in one operation, and the Transaction
/// save fails, you'd have an orphaned Category. Unit of Work ensures either
/// ALL changes save or NONE do — this is transactional consistency.
/// 
/// Think of it like a database transaction wrapper for your repositories.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    ICategoryRepository Categories { get; }
    ITransactionRepository Transactions { get; }
    IBudgetRepository Budgets { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
