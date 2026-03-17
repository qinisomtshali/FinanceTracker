namespace FinanceTracker.Infrastructure.Identity;

/// <summary>
/// Strongly-typed configuration for JWT settings.
/// 
/// OPTIONS PATTERN: Instead of reading configuration values with
/// magic strings like config["Jwt:Key"] scattered everywhere,
/// we bind the entire "Jwt" section of appsettings.json to this class.
/// 
/// Benefits:
///   1. IntelliSense — you get autocomplete for settings
///   2. Compile-time safety — rename a property, compiler catches it
///   3. Single source of truth — all JWT config in one place
///   4. Testable — you can mock IOptions<JwtSettings> in unit tests
/// </summary>
public class JwtSettings
{
	public const string SectionName = "Jwt";

	public string Key { get; set; } = string.Empty;
	public string Issuer { get; set; } = string.Empty;
	public string Audience { get; set; } = string.Empty;
	public int ExpirationInMinutes { get; set; }
	public int RefreshTokenExpirationInDays { get; set; }
}