using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.TimelineEntries.Commands.DeleteTimelineEntry;

public class DeleteTimelineEntryCommandHandler : IRequestHandler<DeleteTimelineEntryCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly ITimelineEntryRepositoryAsync _timelineRepo;
    private readonly IAuthenticatedUserService _authService;

    public DeleteTimelineEntryCommandHandler(
        IApplicationDbContext context,
        ITimelineEntryRepositoryAsync timelineRepo,
        IAuthenticatedUserService authService)
    {
        _context = context;
        _timelineRepo = timelineRepo;
        _authService = authService;
    }

    public async Task<Unit> Handle(DeleteTimelineEntryCommand request, CancellationToken cancellationToken)
    {
        // 1. Load entry
        var entry = await _timelineRepo.GetByIdAsync(request.Id)
            ?? throw new EntityNotFoundException("TimelineEntry", request.Id);

        // 2. Load trip
        var trip = await _context.Trips
            .FirstOrDefaultAsync(t => t.Id == entry.TripId && t.DeletedAt == null, cancellationToken)
            ?? throw new EntityNotFoundException("Trip", entry.TripId);

        // 3. Ownership check
        var currentUserId = Guid.Parse(_authService.UserId);
        if (trip.OwnerId != currentUserId)
            throw new ForbiddenException("You are not authorized to modify this trip.");

        // 4. Draft-only
        if (trip.Status != TripStatus.Draft)
            throw new ApiException("Only draft trips can be modified.");

        // 5. Locked entries cannot be deleted
        if (entry.IsLocked)
            throw new ForbiddenException("Locked timeline entries cannot be deleted.");

        // 6. Soft delete
        entry.DeletedAt = DateTime.UtcNow;

        // LexoRank ordering does not require compaction after deletes.
        // Leaving neighboring order indexes untouched avoids unnecessary writes
        // and prevents delete failures caused by transient reordering conflicts.
        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
