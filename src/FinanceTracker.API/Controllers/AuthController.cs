using FinanceTracker.Application.Common.Interfaces;
using FinanceTracker.Application.DTOs.Auth;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace FinanceTracker.API.Controllers;

/// <summary>
/// Authentication controller — handles register, login, and token refresh.
/// 
/// Notice [AllowAnonymous] is NOT explicitly set here — instead, this controller
/// simply doesn't have [Authorize]. Auth endpoints must be accessible without
/// a token (you can't login if you need to be logged in first!).
/// 
/// This is still a thin controller — it delegates everything to IAuthService.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Register a new user account.
    /// POST /api/auth/register
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        var result = await _authService.RegisterAsync(request);

        return result.IsSuccess
            ? CreatedAtAction(nameof(Register), result.Data)
            : BadRequest(new { errors = result.Errors });
    }

    /// <summary>
    /// Login with email and password.
    /// POST /api/auth/login
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var result = await _authService.LoginAsync(request);

        return result.IsSuccess
            ? Ok(result.Data)
            : BadRequest(new { errors = result.Errors });
    }

    /// <summary>
    /// Refresh an expired access token.
    /// POST /api/auth/refresh
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto request)
    {
        var result = await _authService.RefreshTokenAsync(request.RefreshToken);

        return result.IsSuccess
            ? Ok(result.Data)
            : BadRequest(new { errors = result.Errors });
    }
}