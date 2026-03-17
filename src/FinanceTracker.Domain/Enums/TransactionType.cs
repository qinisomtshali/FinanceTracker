namespace FinanceTracker.Domain.Enums;

/// <summary>
/// Defines whether a transaction is money coming in or going out.
/// 
/// WHY an enum instead of a string: Type safety. The compiler catches typos,
/// and the database stores it as an integer (0 or 1) which is faster to query.
/// </summary>
public enum TransactionType
{
    Expense = 0,
    Income = 1
}
