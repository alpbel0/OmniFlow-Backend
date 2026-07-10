using FluentValidation;

namespace OmniFlow.Application.Features.TripDestinations.Commands.ReorderDestinations;

public class ReorderDestinationsCommandValidator : AbstractValidator<ReorderDestinationsCommand>
{
    public ReorderDestinationsCommandValidator()
    {
        RuleFor(x => x.TripId)
            .NotEmpty().WithMessage("Trip ID is required.");

        RuleFor(x => x.OrderedDestinationIds)
            .NotEmpty().WithMessage("OrderedDestinationIds is required.");

        RuleForEach(x => x.OrderedDestinationIds)
            .NotEmpty().WithMessage("Destination ID is required.");
    }
}
