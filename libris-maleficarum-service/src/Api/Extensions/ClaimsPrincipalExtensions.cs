using System.Security.Claims;

namespace LibrisMaleficarum.Api.Extensions;

/// <summary>
/// Extension methods for <see cref="ClaimsPrincipal"/> to extract user identity information.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// The constant user ID used for anonymous single-user mode.
    /// </summary>
    public const string AnonymousUserId = "_anonymous";

    /// <summary>
    /// Gets the user's object identifier (oid) claim value, or returns the anonymous user ID
    /// if the principal is null, unauthenticated, or has no oid claim.
    /// </summary>
    /// <param name="principal">The claims principal to extract the user ID from.</param>
    /// <returns>The oid claim value or <c>_anonymous</c>.</returns>
    public static string GetUserIdOrAnonymous(this ClaimsPrincipal? principal)
    {
        if (principal is null)
        {
            return AnonymousUserId;
        }

        var oid = principal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
                  ?? principal.FindFirst("oid")?.Value;

        return string.IsNullOrEmpty(oid) ? AnonymousUserId : oid;
    }
}
