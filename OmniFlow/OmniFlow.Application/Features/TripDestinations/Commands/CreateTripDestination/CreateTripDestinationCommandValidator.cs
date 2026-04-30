using FluentValidation;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace OmniFlow.Application.Features.TripDestinations.Commands.CreateTripDestination;

public class CreateTripDestinationCommandValidator : AbstractValidator<CreateTripDestinationCommand>
{
    private readonly IApplicationDbContext _context;

    public CreateTripDestinationCommandValidator(IApplicationDbContext context)
    {
        _context = context;

        RuleFor(x => x.TripId)
            .NotEmpty().WithMessage("Trip ID is required.");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required.");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required.");

        RuleFor(x => x.DepartureDate)
            .GreaterThanOrEqualTo(x => x.ArrivalDate)
            .WithMessage("Departure date must be greater than or equal to arrival date.");

        RuleFor(x => x.OrderIndex)
            .InclusiveBetween(1, 10)
            .WithMessage("OrderIndex must be between 1 and 10.");

        RuleFor(x => x)
            .MustAsync(NotExceedDestinationLimit)
            .WithMessage("A trip can have at most 10 destinations.");
    }

    private async Task<bool> NotExceedDestinationLimit(CreateTripDestinationCommand command, CancellationToken cancellationToken)
    {
        var count = await _context.TripDestinations
            .CountAsync(d => d.TripId == command.TripId && d.DeletedAt == null, cancellationToken);

        return count < 10;
    }
}
