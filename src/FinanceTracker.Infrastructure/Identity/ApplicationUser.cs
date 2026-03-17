using Microsoft.AspNetCore.Identity;

namespace FinanceTracker.Infrastructure.Identity;

/// <summary>
/// The Identity user — extends ASP.NET's IdentityUser with our custom fields.
/// 
/// IMPORTANT: This class lives in Infrastructure, NOT Domain. The Domain has its
/// own User entity that knows nothing about IdentityUser, passwords, or tokens.
/// We map between the two when crossing layer boundaries.
/// 
/// WHY: If we used IdentityUser in the Domain, our domain model would depend on
/// ASP.NET Identity — a framework concern. That would violate Clean Architecture.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
}
