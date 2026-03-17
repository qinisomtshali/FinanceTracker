using System.Security.Claims;
using FinanceTracker.Application.Common.Interfaces;

namespace FinanceTracker.API.Services;

/// <summary>
/// Implements ICurrentUserService by reading the authenticated user's
/// claims from the HTTP context.
/// 
/// When a JWT token is validated, ASP.NET automatically populates
/// HttpContext.User with the claims from the token. We just read them here.
/// 
/// Registered as Scoped — one instance per HTTP request.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return userId != null ? Guid.Parse(userId) : Guid.Empty;
        }
    }

    public string Email =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}
