namespace FinanceTracker.Domain.Entities;

/// <summary>
/// Represents a user in our system.
/// 
/// NOTE: We're NOT inheriting from IdentityUser here on purpose.
/// The Domain layer has ZERO knowledge of ASP.NET Identity — that's
/// an Infrastructure concern. We keep our domain model pure.
/// The Infrastructure layer will map between this and IdentityUser.
/// </summary>
public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties — EF Core uses these to understand relationships
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<Budget> Budgets { get; set; } = new List<Budget>();
}
