using MediatR;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;

namespace OmniFlow.Application.Features.Hotels.Commands.SelectHotel;

public class SelectHotelCommandHandler : IRequestHandler<SelectHotelCommand, Unit>
{
    private readonly ITripRepositoryAsync _tripRepository;
    private readonly IHotelRepositoryAsync _hotelRepository;
    private readonly IAuthenticatedUserService _authenticatedUserService;
    private readonly IApplicationDbContext _context;

    public SelectHotelCommandHandler(
        ITripRepositoryAsync tripRepository,
        IHotelRepositoryAsync hotelRepository,
        IAuthenticatedUserService authenticatedUserService,
        IApplicationDbContext context)
    {
        _tripRepository = tripRepository;
        _hotelRepository = hotelRepository;
        _authenticatedUserService = authenticatedUserService;
        _context = context;
    }

    public async Task<Unit> Handle(SelectHotelCommand request, CancellationToken cancellationToken)
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
            throw new ForbiddenException("You are not authorized to select hotels for this trip.");
        }

        // 3. Get hotel by ID
        var hotel = await _hotelRepository.GetByIdAsync(request.HotelId);

        if (hotel == null)
        {
            throw new EntityNotFoundException("Hotel", request.HotelId);
        }

        // 4. Validate hotel belongs to trip
        if (hotel.TripId != request.TripId)
        {
            throw new ForbiddenException("This hotel does not belong to the specified trip.");
        }

        // 5. If hotel already booked, return success (silent ignore)
        if (hotel.IsBooked)
        {
            return Unit.Value;
        }

        // 6. Atomic Operation (Unit of Work):
        // Cancel ALL previous hotel bookings (no direction filter like flights)
        var previousBookedHotels = await _hotelRepository.GetBookedHotelsByTripAsync(request.TripId);

        foreach (var previousHotel in previousBookedHotels)
        {
            previousHotel.IsBooked = false;
            previousHotel.BookedAt = null;
            // EF Core tracks changes automatically
        }

        // 7. Book selected hotel
        hotel.IsBooked = true;
        hotel.BookedAt = DateTime.UtcNow;

        // 8. Save all changes atomically (single SaveChanges call)
        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}