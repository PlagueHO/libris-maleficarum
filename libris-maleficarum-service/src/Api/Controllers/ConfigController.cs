using LibrisMaleficarum.Domain.Configuration;
using LibrisMaleficarum.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace LibrisMaleficarum.Api.Controllers;

/// <summary>
/// Controller for application configuration endpoints.
/// </summary>
[ApiController]
[Route("api/config")]
public class ConfigController : ControllerBase
{
    private readonly IOptionsMonitor<AccessControlOptions> _accessControlOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigController"/> class.
    /// </summary>
    /// <param name="accessControlOptions">The access control options monitor.</param>
    public ConfigController(IOptionsMonitor<AccessControlOptions> accessControlOptions)
    {
        _accessControlOptions = accessControlOptions ?? throw new ArgumentNullException(nameof(accessControlOptions));
    }

    /// <summary>
    /// Returns whether an access code is required for API requests.
    /// </summary>
    /// <returns>Access control status response.</returns>
    [HttpGet("access-status")]
    [ProducesResponseType(typeof(AccessControlStatusResponse), StatusCodes.Status200OK)]
    public IActionResult GetAccessStatus()
    {
        var response = new AccessControlStatusResponse
        {
            AccessCodeRequired = !string.IsNullOrEmpty(_accessControlOptions.CurrentValue.AccessCode),
        };

        return Ok(response);
    }
}
