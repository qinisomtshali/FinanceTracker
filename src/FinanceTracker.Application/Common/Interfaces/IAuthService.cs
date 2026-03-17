using FinanceTracker.Application.Common.Models;
using FinanceTracker.Application.DTOs.Auth;

namespace FinanceTracker.Application.Common.Interfaces;

/// <summary>
/// Authentication service contract.
/// 
/// CLEAN ARCHITECTURE IN ACTION: The Application layer defines WHAT auth operations
/// it needs (register, login, refresh). It has ZERO knowledge of HOW this happens.
/// JWT? OAuth? Magic links? That's Infrastructure's problem.
/// 
/// This means if you ever switch from JWT to something else, you only change
/// the Infrastructure implementation — Application and Domain stay untouched.
/// </summary>
public interface IAuthService
{
    Task<Result<AuthResponseDto>> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);
    Task<Result<AuthResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task<Result<AuthResponseDto>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
}
