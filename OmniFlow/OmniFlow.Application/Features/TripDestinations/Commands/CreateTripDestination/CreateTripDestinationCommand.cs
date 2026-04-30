using MediatR;

namespace OmniFlow.Application.Features.TripDestinations.Commands.CreateTripDestination;

public class CreateTripDestinationCommand : IRequest<Guid>
{
    public Guid TripId { get; set; }
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public DateOnly ArrivalDate { get; set; }
    public DateOnly DepartureDate { get; set; }
    public int OrderIndex { get; set; }
}
