namespace LibrisMaleficarum.Api.Models.Responses;

/// <summary>
/// Represents a field-level validation error.
/// </summary>
public class ValidationError
{
    /// <summary>
    /// Gets or sets the name of the field that failed validation.
    /// </summary>
    public required string Field { get; set; }

    /// <summary>
    /// Gets or sets the validation error message for this field.
    /// </summary>
    public required string Message { get; set; }
}
