using FluentValidation;

namespace OmniFlow.Application.Features.Places.Commands.CreatePlace;

public class CreatePlaceCommandValidator : AbstractValidator<CreatePlaceCommand>
{
    public CreatePlaceCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(255).WithMessage("Name must not exceed 255 characters.");

        RuleFor(x => x.Category)
            .IsInEnum().WithMessage("Invalid category value.");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required.");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required.");

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90.0, 90.0).WithMessage("Latitude must be between -90 and 90.");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180.0, 180.0).WithMessage("Longitude must be between -180 and 180.");

        RuleFor(x => x.Rating)
            .InclusiveBetween(1m, 5m).WithMessage("Rating must be between 1 and 5.");

        RuleFor(x => x.EstimatedPrice)
            .InclusiveBetween(0m, decimal.MaxValue).WithMessage("Estimated price must be greater than or equal to 0.");

        RuleFor(x => x.CurrencyCode)
            .Matches(@"^[A-Z]{3}$").WithMessage("Currency code must be exactly 3 uppercase letters.");
    }
}