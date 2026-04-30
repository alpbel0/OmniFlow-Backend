using MediatR;

namespace OmniFlow.Application.Features.TripDestinations.Commands.UpdateTripDestination;

public class UpdateTripDestinationCommand : IRequest<Unit>
{
    public Guid DestinationId { get; set; }
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public DateOnly ArrivalDate { get; set; }
    public DateOnly DepartureDate { get; set; }
    public int OrderIndex { get; set; }
}
