using MediatR;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.SavedTrips.Commands.SaveTrip;

/// <summary>
/// Handler for saving a trip to user's saved list.
/// Business rules:
/// - Trip must exist
/// - Trip must be Published
/// - Self-save is allowed (users can save their own trips)
/// - Duplicate saves are silently ignored (no exception)
/// </summary>
public class SaveTripCommandHandler : IRequestHandler<SaveTripCommand, Unit>
{
    private readonly IGenericRepositoryAsync<Trip> _tripRepository;
    private readonly IApplicationDbContext _context;
    private readonly IAuthenticatedUserService _authenticatedUserService;

    public SaveTripCommandHandler(
        IGenericRepositoryAsync<Trip> tripRepository,
        IApplicationDbContext context,
        IAuthenticatedUserService authenticatedUserService)
    {
        _tripRepository = tripRepository;
        _context = context;
        _authenticatedUserService = authenticatedUserService;
    }

    public async Task<Unit> Handle(SaveTripCommand request, CancellationToken cancellationToken)
    {
        // 1. Get trip
        var trip = await _tripRepository.GetByIdAsync(request.TripId);

        // 2. Check trip exists
        if (trip == null)
        {
            throw new EntityNotFoundException("Trip", request.TripId);
        }

        // 3. Check trip status (must be Published or Draft)
        if (trip.Status != TripStatus.Published && trip.Status != TripStatus.Draft)
        {
            throw new ApiException("Only published or draft trips can be saved.", 400);
        }

        // 4. Get current user ID
        var userId = Guid.Parse(_authenticatedUserService.UserId);

        // 5. Check for duplicate save (silently ignore)
        var existingSave = await _context.SavedTrips
            .FindAsync(new object[] { userId, request.TripId }, cancellationToken);

        if (existingSave != null)
        {
            // Silent ignore - return without creating duplicate
            return Unit.Value;
        }

        // 6. Create saved trip
        var savedTrip = new SavedTrip
        {
            UserId = userId,
            TripId = request.TripId,
            CreatedAt = DateTime.UtcNow
        };

        _context.SavedTrips.Add(savedTrip);

        // 7. Save changes
        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}