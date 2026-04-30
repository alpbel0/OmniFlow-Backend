using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Enums;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Application.Features.TimelineEntries.Commands.ReorderTimelineEntries;

public class ReorderTimelineEntriesCommandHandler : IRequestHandler<ReorderTimelineEntriesCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly ITimelineEntryRepositoryAsync _timelineRepo;
    private readonly ITimelineService _timelineService;
    private readonly IAuthenticatedUserService _authService;

    public ReorderTimelineEntriesCommandHandler(
        IApplicationDbContext context,
        ITimelineEntryRepositoryAsync timelineRepo,
        ITimelineService timelineService,
        IAuthenticatedUserService authService)
    {
        _context = context;
        _timelineRepo = timelineRepo;
        _timelineService = timelineService;
        _authService = authService;
    }

    public async Task<Unit> Handle(ReorderTimelineEntriesCommand request, CancellationToken cancellationToken)
    {
        // 1. Load entry
        var entry = await _timelineRepo.GetByIdAsync(request.EntryId)
            ?? throw new EntityNotFoundException("TimelineEntry", request.EntryId);

        // 2. Load trip
        var trip = await _context.Trips
            .FirstOrDefaultAsync(t => t.Id == request.TripId && t.DeletedAt == null, cancellationToken)
            ?? throw new EntityNotFoundException("Trip", request.TripId);

        // 3. Ownership check
        var currentUserId = Guid.Parse(_authService.UserId);
        if (trip.OwnerId != currentUserId)
            throw new ForbiddenException("You are not authorized to modify this trip.");

        // 4. Draft-only
        if (trip.Status != TripStatus.Draft)
            throw new ApiException("Only draft trips can be modified.");

        // 5. Load all entries for the trip
        var entries = await _context.TimelineEntries
            .Where(e => e.TripId == request.TripId && e.DeletedAt == null)
            .ToListAsync(cancellationToken);

        // 6. Find before/after entries
        TimelineEntry? beforeEntry = null;
        TimelineEntry? afterEntry = null;

        if (request.BeforeEntryId.HasValue)
        {
            beforeEntry = entries.FirstOrDefault(e => e.Id == request.BeforeEntryId.Value)
                ?? throw new EntityNotFoundException("TimelineEntry (BeforeEntry)", request.BeforeEntryId.Value);
        }

        if (request.AfterEntryId.HasValue)
        {
            afterEntry = entries.FirstOrDefault(e => e.Id == request.AfterEntryId.Value)
                ?? throw new EntityNotFoundException("TimelineEntry (AfterEntry)", request.AfterEntryId.Value);
        }

        // 7. Same destination and day validation
        if (beforeEntry != null && (beforeEntry.DestinationId != entry.DestinationId || beforeEntry.DayNumber != entry.DayNumber))
            throw new ApiException("BeforeEntry must belong to the same destination and day.");

        if (afterEntry != null && (afterEntry.DestinationId != entry.DestinationId || afterEntry.DayNumber != entry.DayNumber))
            throw new ApiException("AfterEntry must belong to the same destination and day.");

        // 8. Calculate new LexoRank
        entry.OrderIndex = _timelineService.GetLexoRankBetween(afterEntry?.OrderIndex, beforeEntry?.OrderIndex);

        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
