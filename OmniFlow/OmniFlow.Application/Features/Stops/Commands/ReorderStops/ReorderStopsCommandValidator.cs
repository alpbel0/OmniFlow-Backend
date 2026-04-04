using FluentValidation;

namespace OmniFlow.Application.Features.Stops.Commands.ReorderStops;

public class ReorderStopsCommandValidator : AbstractValidator<ReorderStopsCommand>
{
    public ReorderStopsCommandValidator()
    {
        RuleFor(x => x.TripId)
            .NotEmpty().WithMessage("TripId is required.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("At least one reorder item is required.");

        RuleForEach(x => x.Items).SetValidator(new ReorderStopItemValidator());
    }
}

public class ReorderStopItemValidator : AbstractValidator<ReorderStopItem>
{
    public ReorderStopItemValidator()
    {
        RuleFor(x => x.StopId)
            .NotEmpty().WithMessage("StopId is required.");

        RuleFor(x => x.NewDayNumber)
            .GreaterThan(0).WithMessage("NewDayNumber must be greater than 0.");
    }
}