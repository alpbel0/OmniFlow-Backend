using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;

namespace OmniFlow.Application.Features.TimelineEntries.Commands.MarkEntryVisited;

public class MarkEntryVisitedCommandHandler : IRequestHandler<MarkEntryVisitedCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly ITimelineEntryRepositoryAsync _timelineRepo;
    private readonly IAuthenticatedUserService _authService;

    public MarkEntryVisitedCommandHandler(
        IApplicationDbContext context,
        ITimelineEntryRepositoryAsync timelineRepo,
        IAuthenticatedUserService authService)
    {
        _context = context;
        _timelineRepo = timelineRepo;
        _authService = authService;
    }

    public async Task<Unit> Handle(MarkEntryVisitedCommand request, CancellationToken cancellationToken)
    {
        // 1. Load entry
        var entry = await _timelineRepo.GetByIdAsync(request.EntryId)
            ?? throw new EntityNotFoundException("TimelineEntry", request.EntryId);

        // 2. Load trip
        var trip = await _context.Trips
            .FirstOrDefaultAsync(t => t.Id == entry.TripId && t.DeletedAt == null, cancellationToken)
            ?? throw new EntityNotFoundException("Trip", entry.TripId);

        // 3. Ownership check (both Draft and Published trips)
        var currentUserId = Guid.Parse(_authService.UserId);
        if (trip.OwnerId != currentUserId)
            throw new ForbiddenException("You are not authorized to modify this trip.");

        // 4. No-op if state hasn't changed
        if (entry.IsVisited == request.IsVisited)
            return Unit.Value;

        // 5. Update visited state
        if (request.IsVisited)
            entry.MarkVisited();
        else
            entry.MarkUnvisited();

        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
