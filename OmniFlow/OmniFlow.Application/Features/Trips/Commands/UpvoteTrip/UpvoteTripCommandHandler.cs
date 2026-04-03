using MediatR;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;
using OmniFlow.Domain.Exceptions;

namespace OmniFlow.Application.Features.Trips.Commands.UpvoteTrip;

/// <summary>
/// Handler for upvoting a published trip.
/// Business rules:
/// - Trip must exist
/// - Trip must be Published
/// - User cannot upvote their own trip (SelfUpvoteException)
/// - Duplicate upvote throws DuplicateUpvoteException
/// </summary>
public class UpvoteTripCommandHandler : IRequestHandler<UpvoteTripCommand, Unit>
{
    private readonly ITripRepositoryAsync _tripRepository;
    private readonly IApplicationDbContext _context;
    private readonly IAuthenticatedUserService _authenticatedUserService;

    public UpvoteTripCommandHandler(
        ITripRepositoryAsync tripRepository,
        IApplicationDbContext context,
        IAuthenticatedUserService authenticatedUserService)
    {
        _tripRepository = tripRepository;
        _context = context;
        _authenticatedUserService = authenticatedUserService;
    }

    public async Task<Unit> Handle(UpvoteTripCommand request, CancellationToken cancellationToken)
    {
        // 1. Get trip with owner
        var trip = await _tripRepository.GetByIdWithOwnerAsync(request.TripId);

        // 2. Check trip exists
        if (trip == null)
        {
            throw new EntityNotFoundException("Trip", request.TripId);
        }

        // 3. Check trip status (must be Published)
        if (trip.Status != TripStatus.Published)
        {
            throw new ApiException("Only published trips can be upvoted.", 400);
        }

        // 4. Get current user ID
        var userId = Guid.Parse(_authenticatedUserService.UserId);

        // 5. Self-upvote check
        if (trip.OwnerId == userId)
        {
            throw new SelfUpvoteException(userId);
        }

        // 6. Check for duplicate upvote
        var existingUpvote = await _context.TripUpvotes
            .FindAsync(new object[] { request.TripId, userId }, cancellationToken);

        if (existingUpvote != null)
        {
            throw new DuplicateUpvoteException("trip", request.TripId);
        }

        // 7. Create upvote
        var upvote = new TripUpvote
        {
            TripId = request.TripId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.TripUpvotes.Add(upvote);

        // 8. Increment upvote count
        trip.UpvoteCount++;

        // 9. Save changes
        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}