using MediatR;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Application.Features.Trips.Commands.RemoveUpvoteTrip;

/// <summary>
/// Handler for removing an upvote from a trip.
/// Business rules:
/// - Trip must exist
/// - User must have previously upvoted the trip
/// - UpvoteCount decrements (clamped to >= 0)
/// </summary>
public class RemoveUpvoteTripCommandHandler : IRequestHandler<RemoveUpvoteTripCommand, Unit>
{
    private readonly IGenericRepositoryAsync<Trip> _tripRepository;
    private readonly IApplicationDbContext _context;
    private readonly IAuthenticatedUserService _authenticatedUserService;

    public RemoveUpvoteTripCommandHandler(
        IGenericRepositoryAsync<Trip> tripRepository,
        IApplicationDbContext context,
        IAuthenticatedUserService authenticatedUserService)
    {
        _tripRepository = tripRepository;
        _context = context;
        _authenticatedUserService = authenticatedUserService;
    }

    public async Task<Unit> Handle(RemoveUpvoteTripCommand request, CancellationToken cancellationToken)
    {
        // 1. Get trip
        var trip = await _tripRepository.GetByIdAsync(request.TripId);

        // 2. Check trip exists
        if (trip == null)
        {
            throw new EntityNotFoundException("Trip", request.TripId);
        }

        // 3. Get current user ID
        var userId = Guid.Parse(_authenticatedUserService.UserId);

        // 4. Find existing upvote
        var existingUpvote = await _context.TripUpvotes
            .FindAsync(new object[] { request.TripId, userId }, cancellationToken);

        // 5. Check upvote exists
        if (existingUpvote == null)
        {
            throw new EntityNotFoundException("TripUpvote", request.TripId);
        }

        // 6. Remove upvote
        _context.TripUpvotes.Remove(existingUpvote);

        // 7. Decrement upvote count (ensure >= 0)
        trip.UpvoteCount = Math.Max(0, trip.UpvoteCount - 1);

        // 8. Save changes
        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}