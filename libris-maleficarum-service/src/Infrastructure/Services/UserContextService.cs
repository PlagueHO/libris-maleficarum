using LibrisMaleficarum.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Http;

namespace LibrisMaleficarum.Infrastructure.Services;

/// <summary>
/// Implementation of <see cref="IUserContextService"/> that reads the current user identity
/// from the <see cref="HttpContext.User"/> claims principal via <see cref="IHttpContextAccessor"/>.
/// </summary>
/// <remarks>
/// In single-user anonymous mode, the <see cref="Api.Middleware.AnonymousClaimsMiddleware"/>
/// injects synthetic claims with oid = _anonymous, so this service consistently returns
/// the user identity regardless of auth mode.
/// </remarks>
public class UserContextService(IHttpContextAccessor httpContextAccessor) : IUserContextService
{
    private const string AnonymousUserId = "_anonymous";

    /// <inheritdoc />
    public Task<string> GetCurrentUserIdAsync()
    {
        var principal = httpContextAccessor.HttpContext?.User;
        if (principal is null)
        {
            return Task.FromResult(AnonymousUserId);
        }

        var oid = principal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
                  ?? principal.FindFirst("oid")?.Value;

        return Task.FromResult(string.IsNullOrEmpty(oid) ? AnonymousUserId : oid);
    }
}
