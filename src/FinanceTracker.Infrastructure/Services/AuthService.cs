using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FinanceTracker.Application.Common.Interfaces;
using FinanceTracker.Application.Common.Models;
using FinanceTracker.Application.DTOs.Auth;
using FinanceTracker.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FinanceTracker.Infrastructure.Services;

/// <summary>
/// The IMPLEMENTATION of IAuthService — this is where the real auth magic happens.
/// 
/// Remember: The Application layer defined the IAuthService interface saying
/// "I need register, login, and refresh capabilities." This class delivers
/// the HOW using ASP.NET Identity + JWT.
/// 
/// DEPENDENCY INVERSION IN ACTION:
///   - Application layer depends on IAuthService (abstraction)
///   - This class depends on IAuthService (implements it)
///   - Neither depends on the other directly
///   - They're connected at runtime via DI registration
/// </summary>
public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        IOptions<JwtSettings> jwtSettings)
    {
        _userManager = userManager;
        _jwtSettings = jwtSettings.Value;
    }

    /// <summary>
    /// Register a new user.
    /// 
    /// FLOW:
    ///   1. Check if email already exists
    ///   2. Create the Identity user (Identity handles password hashing)
    ///   3. Generate JWT + refresh token
    ///   4. Return tokens to the client
    /// 
    /// SECURITY NOTE: Identity uses PBKDF2 with HMAC-SHA256 for password hashing.
    /// This is a slow, salted hash — exactly what you want for passwords.
    /// Never store plain text passwords. Never roll your own hashing.
    /// </summary>
    public async Task<Result<AuthResponseDto>> RegisterAsync(
        RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
            return Result<AuthResponseDto>.Failure("An account with this email already exists.");

        // Create the Identity user
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            UserName = request.Email, // Identity requires UserName
            FirstName = request.FirstName,
            LastName = request.LastName,
            CreatedAt = DateTime.UtcNow
        };

        // CreateAsync hashes the password and saves the user
        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return Result<AuthResponseDto>.Failure(errors);
        }

        // Generate tokens
        var authResponse = await GenerateAuthResponse(user);
        return Result<AuthResponseDto>.Success(authResponse);
    }

    /// <summary>
    /// Login with email and password.
    /// 
    /// FLOW:
    ///   1. Find user by email
    ///   2. Verify password against stored hash
    ///   3. Check if account is locked out
    ///   4. Generate JWT + refresh token
    /// 
    /// SECURITY: We return the same generic error for "user not found" and
    /// "wrong password". This prevents attackers from discovering which
    /// emails are registered (enumeration attack).
    /// </summary>
    public async Task<Result<AuthResponseDto>> LoginAsync(
        LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return Result<AuthResponseDto>.Failure("Invalid email or password.");

        // Check if account is locked out
        if (await _userManager.IsLockedOutAsync(user))
            return Result<AuthResponseDto>.Failure(
                "Account is locked. Please try again later.");

        // Verify password
        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            // Record failed attempt (for lockout tracking)
            await _userManager.AccessFailedAsync(user);
            return Result<AuthResponseDto>.Failure("Invalid email or password.");
        }

        // Reset failed attempts on successful login
        await _userManager.ResetAccessFailedCountAsync(user);

        var authResponse = await GenerateAuthResponse(user);
        return Result<AuthResponseDto>.Success(authResponse);
    }

    /// <summary>
    /// Refresh an expired access token using a valid refresh token.
    /// 
    /// WHY REFRESH TOKENS: JWTs are stateless — the server can't invalidate them.
    /// So we make access tokens short-lived (60 min) and use longer-lived refresh
    /// tokens (7 days) to get new access tokens without re-entering credentials.
    /// 
    /// If a refresh token is compromised, the damage window is limited and
    /// we can invalidate it by clearing it from the database.
    /// </summary>
    public async Task<Result<AuthResponseDto>> RefreshTokenAsync(
        string refreshToken, CancellationToken cancellationToken = default)
    {
        // Find user with this refresh token
        // Note: In production, you'd want an index on RefreshToken for performance
        var users = _userManager.Users;
        var user = users.FirstOrDefault(u => u.RefreshToken == refreshToken);

        if (user == null)
            return Result<AuthResponseDto>.Failure("Invalid refresh token.");

        if (user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            return Result<AuthResponseDto>.Failure(
                "Refresh token has expired. Please login again.");

        var authResponse = await GenerateAuthResponse(user);
        return Result<AuthResponseDto>.Success(authResponse);
    }

    // ================================================================
    // PRIVATE HELPER METHODS
    // ================================================================

    /// <summary>
    /// Generate JWT access token + refresh token for a user.
    /// Also saves the refresh token to the database.
    /// </summary>
    private async Task<AuthResponseDto> GenerateAuthResponse(ApplicationUser user)
    {
        var token = GenerateJwtToken(user);
        var refreshToken = GenerateRefreshToken();
        var expiration = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes);

        // Save refresh token to database
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(
            _jwtSettings.RefreshTokenExpirationInDays);
        await _userManager.UpdateAsync(user);

        var userDto = new UserDto(user.Id, user.Email!, user.FirstName, user.LastName);

        return new AuthResponseDto(token, refreshToken, expiration, userDto);
    }

    /// <summary>
    /// Generate a JWT (JSON Web Token).
    /// 
    /// ANATOMY OF A JWT:
    ///   Header.Payload.Signature
    /// 
    ///   Header: {"alg": "HS256", "typ": "JWT"}
    ///   Payload: {"sub": "user-id", "email": "...", "exp": 1234567890}
    ///   Signature: HMACSHA256(base64(header) + "." + base64(payload), secret)
    /// 
    /// The CLAIMS in the payload are what the server reads to identify the user.
    /// When a request comes in with a JWT, ASP.NET validates the signature,
    /// checks expiration, and populates HttpContext.User with these claims.
    /// 
    /// That's how CurrentUserService.UserId works — it reads ClaimTypes.NameIdentifier
    /// from the validated JWT claims.
    /// </summary>
    private string GenerateJwtToken(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            // Sub (subject) = user ID — this is the primary identifier
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            // Email claim
            new(ClaimTypes.Email, user.Email!),
            // Name for display purposes
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            // JTI (JWT ID) = unique token identifier, useful for token revocation
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Generate a cryptographically secure random refresh token.
    /// 
    /// WHY NOT JUST ANOTHER JWT: Refresh tokens are opaque strings stored in the DB.
    /// Unlike JWTs (which are self-contained and can't be invalidated), refresh tokens
    /// can be revoked by simply deleting them from the database.
    /// </summary>
    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}