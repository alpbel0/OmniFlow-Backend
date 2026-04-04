using MediatR;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.Flights.Commands.SelectFlight;

public class SelectFlightCommandHandler : IRequestHandler<SelectFlightCommand, Unit>
{
    private readonly ITripRepositoryAsync _tripRepository;
    private readonly IFlightRepositoryAsync _flightRepository;
    private readonly IAuthenticatedUserService _authenticatedUserService;
    private readonly IApplicationDbContext _context;

    public SelectFlightCommandHandler(
        ITripRepositoryAsync tripRepository,
        IFlightRepositoryAsync flightRepository,
        IAuthenticatedUserService authenticatedUserService,
        IApplicationDbContext context)
    {
        _tripRepository = tripRepository;
        _flightRepository = flightRepository;
        _authenticatedUserService = authenticatedUserService;
        _context = context;
    }

    public async Task<Unit> Handle(SelectFlightCommand request, CancellationToken cancellationToken)
    {
        // 1. Get trip with owner for authorization
        var trip = await _tripRepository.GetByIdWithOwnerAsync(request.TripId);

        if (trip == null)
        {
            throw new EntityNotFoundException("Trip", request.TripId);
        }

        // 2. Owner authorization
        var currentUserId = Guid.Parse(_authenticatedUserService.UserId);
        if (trip.OwnerId != currentUserId)
        {
            throw new ForbiddenException("You are not authorized to select flights for this trip.");
        }

        // 3. Get flight by ID
        var flight = await _flightRepository.GetByIdAsync(request.FlightId);

        if (flight == null)
        {
            throw new EntityNotFoundException("Flight", request.FlightId);
        }

        // 4. Validate flight belongs to trip
        if (flight.TripId != request.TripId)
        {
            throw new ForbiddenException("This flight does not belong to the specified trip.");
        }

        // 5. If flight already booked, return success (silent ignore)
        if (flight.IsBooked)
        {
            return Unit.Value;
        }

        // 6. Atomic Operation (Unit of Work):
        // Cancel previous bookings in same direction
        var previousBookedFlights = await _flightRepository.GetBookedFlightsByDirectionAsync(
            request.TripId, flight.FlightDirection);

        foreach (var previousFlight in previousBookedFlights)
        {
            previousFlight.IsBooked = false;
            previousFlight.BookedAt = null;
            // EF Core tracks changes automatically
        }

        // 7. Book selected flight
        flight.IsBooked = true;
        flight.BookedAt = DateTime.UtcNow;

        // 8. Save all changes atomically (single SaveChanges call)
        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}