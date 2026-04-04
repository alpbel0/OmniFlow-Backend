using FluentValidation;

namespace OmniFlow.Application.Features.Hotels.Commands.SelectHotel;

public class SelectHotelCommandValidator : AbstractValidator<SelectHotelCommand>
{
    public SelectHotelCommandValidator()
    {
        RuleFor(x => x.TripId).NotEmpty().WithMessage("TripId is required.");
        RuleFor(x => x.HotelId).NotEmpty().WithMessage("HotelId is required.");
    }
}