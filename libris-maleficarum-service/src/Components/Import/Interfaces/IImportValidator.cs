namespace LibrisMaleficarum.Import.Interfaces;

/// <summary>
/// Validates parsed import source content and produces a resolved manifest.
/// </summary>
public interface IImportValidator
{
    /// <summary>
    /// Validates the import source content and returns a validation result with any errors, warnings, and the resolved manifest.
    /// </summary>
    /// <param name="content">The parsed import source content to validate.</param>
    /// <returns>The validation result.</returns>
    Models.ImportValidationResult Validate(Models.ImportSourceContent content);
}
