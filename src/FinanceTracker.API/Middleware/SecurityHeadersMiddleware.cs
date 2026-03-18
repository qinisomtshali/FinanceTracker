using System.Threading.Tasks;

namespace FinanceTracker.API.Middleware;

/// <summary>
/// Adds security headers to every HTTP response.
/// 
/// These headers tell browsers to enable various security features:
///   - X-Content-Type-Options: Prevents MIME type sniffing
///   - X-Frame-Options: Prevents clickjacking (embedding your site in an iframe)
///   - X-XSS-Protection: Enables browser's XSS filter
///   - Referrer-Policy: Controls how much URL info is sent to other sites
///   - Content-Security-Policy: Controls which resources the browser can load
///   - Strict-Transport-Security: Forces HTTPS for future requests
///
/// These are free security wins — just headers, no code changes needed.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'");
        context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");

        await _next(context);
    }
}