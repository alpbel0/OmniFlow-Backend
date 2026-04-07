using FluentValidation;

namespace OmniFlow.Application.DTOs.Hotels;

public class SelectHotelRequestValidator : AbstractValidator<SelectHotelRequest>
{
    public SelectHotelRequestValidator()
    {
        RuleFor(x => x.HotelId)
            .NotEmpty().WithMessage("HotelId is required.");
    }
}