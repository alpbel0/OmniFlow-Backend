using MediatR;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Application.Features.SavedTrips.Commands.UnsaveTrip;

/// <summary>
/// Handler for removing a trip from user's saved list.
/// Business rules:
/// - SavedTrip record must exist for (UserId, TripId)
/// </summary>
public class UnsaveTripCommandHandler : IRequestHandler<UnsaveTripCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthenticatedUserService _authenticatedUserService;

    public UnsaveTripCommandHandler(
        IApplicationDbContext context,
        IAuthenticatedUserService authenticatedUserService)
    {
        _context = context;
        _authenticatedUserService = authenticatedUserService;
    }

    public async Task<Unit> Handle(UnsaveTripCommand request, CancellationToken cancellationToken)
    {
        // 1. Get current user ID
        var userId = Guid.Parse(_authenticatedUserService.UserId);

        // 2. Find existing saved trip
        var existingSave = await _context.SavedTrips
            .FindAsync(new object[] { userId, request.TripId }, cancellationToken);

        // 3. Check saved trip exists
        if (existingSave == null)
        {
            throw new EntityNotFoundException("SavedTrip", request.TripId);
        }

        // 4. Remove saved trip
        _context.SavedTrips.Remove(existingSave);

        // 5. Save changes
        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}