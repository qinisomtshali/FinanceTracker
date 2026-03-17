namespace FinanceTracker.Application.DTOs.Auth;

/// <summary>
/// DTOs (Data Transfer Objects) are the shapes of data that cross the API boundary.
/// 
/// WHY NOT USE ENTITIES DIRECTLY:
///   1. Security — entities may have fields you don't want exposed (PasswordHash)
///   2. Decoupling — API contract stays stable even if entity changes
///   3. Flexibility — response shape can differ from storage shape
///   4. Validation — DTOs can have different validation than entities
/// 
/// GOLDEN RULE: Never return a domain entity from an API endpoint. Always map to a DTO.
/// </summary>

public record RegisterRequestDto(
    string Email,
    string Password,
    string FirstName,
    string LastName
);

public record LoginRequestDto(
    string Email,
    string Password
);

public record AuthResponseDto(
    string Token,
    string RefreshToken,
    DateTime Expiration,
    UserDto User
);

public record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName
);

public record RefreshTokenRequestDto(
    string RefreshToken
);