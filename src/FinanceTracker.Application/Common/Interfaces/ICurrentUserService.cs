namespace FinanceTracker.Application.Common.Interfaces;

/// <summary>
/// Provides access to the currently authenticated user's information.
/// 
/// WHY: Controllers and handlers need to know WHO is making the request
/// to filter data by UserId. But the Application layer can't access
/// HttpContext directly (that's an ASP.NET concern). So we define this
/// interface here, and the API/Infrastructure layer implements it by
/// reading the JWT claims from HttpContext.
/// 
/// This is another example of Dependency Inversion in practice.
/// </summary>
public interface ICurrentUserService
{
    Guid UserId { get; }
    string Email { get; }
    bool IsAuthenticated { get; }
}
