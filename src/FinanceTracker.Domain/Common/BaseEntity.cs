namespace FinanceTracker.Domain.Common;

/// <summary>
/// Base class for all domain entities.
/// Every entity in our system has an Id and audit timestamps.
/// 
/// WHY: This avoids repeating Id/CreatedAt/UpdatedAt in every entity.
/// It also gives us a single place to add domain events later.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
