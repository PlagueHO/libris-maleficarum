using FluentValidation;
using LibrisMaleficarum.Api.Models.Requests;

namespace LibrisMaleficarum.Api.Validators;

/// <summary>
/// Validator for <see cref="UpdateWorldRequest"/>.
/// </summary>
public class UpdateWorldRequestValidator : AbstractValidator<UpdateWorldRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateWorldRequestValidator"/> class.
    /// </summary>
    public UpdateWorldRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .Length(1, 100)
            .WithMessage("Name must be between 1 and 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .WithMessage("Description must not exceed 2000 characters.")
            .When(x => x.Description is not null);
    }
}
