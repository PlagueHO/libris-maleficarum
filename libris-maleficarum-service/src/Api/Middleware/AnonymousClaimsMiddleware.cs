using System.Security.Claims;

namespace LibrisMaleficarum.Api.Middleware;

/// <summary>
/// Middleware that injects a synthetic anonymous <see cref="ClaimsPrincipal"/>
/// when the request has no authenticated identity. Used in single-user mode
/// when Entra ID is not configured.
/// </summary>
public sealed class AnonymousClaimsMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnonymousClaimsMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    public AnonymousClaimsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Processes the request, injecting anonymous claims if no authenticated identity is present.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity is null || !context.User.Identity.IsAuthenticated)
        {
            var claims = new[]
            {
                new Claim("oid", "_anonymous"),
                new Claim("scp", "access_as_user"),
            };

            context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Anonymous"));
        }

        await _next(context);
    }
}
