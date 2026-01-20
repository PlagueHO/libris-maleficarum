namespace LibrisMaleficarum.Api.Validators;

using FluentValidation;
using LibrisMaleficarum.Api.Models.Requests;
using LibrisMaleficarum.Domain.ValueObjects;
using System.Text.Json;

/// <summary>
/// Validator for CreateWorldEntityRequest.
/// </summary>
public class CreateWorldEntityRequestValidator : AbstractValidator<CreateWorldEntityRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateWorldEntityRequestValidator"/> class.
    /// </summary>
    public CreateWorldEntityRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Entity name is required.")
            .Length(1, 200)
            .WithMessage("Entity name must be between 1 and 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(5000)
            .WithMessage("Description must not exceed 5000 characters.")
            .When(x => x.Description is not null);

        RuleFor(x => x.EntityType)
            .IsInEnum()
            .WithMessage("Invalid entity type.");

        RuleFor(x => x.Tags)
            .Must(tags => tags == null || tags.Count <= 20)
            .WithMessage("Maximum 20 tags allowed.")
            .Must(tags => tags == null || tags.All(t => !string.IsNullOrWhiteSpace(t) && t.Length <= 50))
            .WithMessage("Each tag must be 1-50 characters.")
            .When(x => x.Tags is not null);

        RuleFor(x => x.Attributes)
            .Must(attributes => attributes == null || ValidateAttributesSize(attributes))
            .WithMessage("Attributes must not exceed 100KB serialized.")
            .When(x => x.Attributes is not null);
    }

    private static bool ValidateAttributesSize(Dictionary<string, object> attributes)
    {
        try
        {
            var json = JsonSerializer.Serialize(attributes);
            var sizeBytes = System.Text.Encoding.UTF8.GetByteCount(json);
            return sizeBytes <= 100 * 1024; // 100KB
        }
        catch
        {
            return false;
        }
    }
}
