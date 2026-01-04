using LibrisMaleficarum.Domain.Interfaces.Services;

namespace LibrisMaleficarum.Infrastructure.Services;

/// <summary>
/// Stubbed implementation of <see cref="IUserContextService"/> for local development.
/// Returns a hardcoded GUID for the current user.
/// </summary>
/// <remarks>
/// In production, this will be replaced with a real implementation that retrieves
/// the authenticated user ID from Azure Entra ID (formerly Azure AD) claims.
/// </remarks>
public class UserContextService : IUserContextService
{
    /// <summary>
    /// Hardcoded user ID for local development and testing.
    /// </summary>
    private static readonly Guid StubUserId = new("00000000-0000-0000-0000-000000000001");

    /// <inheritdoc />
    public Task<Guid> GetCurrentUserIdAsync()
    {
        return Task.FromResult(StubUserId);
    }
}
