using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.TimelineEntries.Commands.MarkEntryVisited;

public class MarkEntryVisitedCommandHandler : IRequestHandler<MarkEntryVisitedCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly ITimelineEntryRepositoryAsync _timelineRepo;
    private readonly IAuthenticatedUserService _authService;
    private readonly ITripTemporalService? _temporalService;
    private readonly IDateTimeService? _dateTimeService;

    public MarkEntryVisitedCommandHandler(
        IApplicationDbContext context,
        ITimelineEntryRepositoryAsync timelineRepo,
        IAuthenticatedUserService authService,
        ITripTemporalService? temporalService = null,
        IDateTimeService? dateTimeService = null)
    {
        _context = context;
        _timelineRepo = timelineRepo;
        _authService = authService;
        _temporalService = temporalService;
        _dateTimeService = dateTimeService;
    }

    public async Task<Unit> Handle(MarkEntryVisitedCommand request, CancellationToken cancellationToken)
    {
        // 1. Load entry
        var entry = await _timelineRepo.GetByIdAsync(request.EntryId)
            ?? throw new EntityNotFoundException("TimelineEntry", request.EntryId);

        // 2. Load trip
        var trip = await _context.Trips
            .Include(t => t.Destinations)
            .FirstOrDefaultAsync(t => t.Id == entry.TripId && t.DeletedAt == null, cancellationToken)
            ?? throw new EntityNotFoundException("Trip", entry.TripId);

        // 3. Ownership check (both Draft and Published trips)
        var currentUserId = Guid.Parse(_authService.UserId);
        if (trip.OwnerId != currentUserId)
            throw new ForbiddenException("You are not authorized to modify this trip.");

        if (_temporalService is not null && _dateTimeService is not null)
        {
            await HandleVisitLogStateAsync(entry, trip, request.IsVisited, currentUserId, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }

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

    private async Task HandleVisitLogStateAsync(
        TimelineEntry entry,
        Trip trip,
        bool isVisited,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (entry.EntryType is not (TimelineEntryType.Place or TimelineEntryType.CustomEvent))
            throw new ApiException("This timeline entry type cannot be visited.", 422, "VISIT_LOG_UNSUPPORTED_ENTRY_TYPE");

        if (!isVisited)
        {
            var logToDelete = await _context.PlaceVisitLogs
                .FirstOrDefaultAsync(x => x.TimelineEntryId == entry.Id, cancellationToken);
            if (logToDelete is not null)
                logToDelete.DeletedAt = _dateTimeService!.NowUtc;
            entry.MarkUnvisited();
            return;
        }

        var execution = _temporalService!.GetExecutionState(trip);
        if (!execution.IsTimezoneComplete)
            throw new ApiException("Trip timezone information is incomplete.", 409, "TIMEZONE_UNAVAILABLE");
        if (execution.State == TripExecutionState.Upcoming)
            throw new ApiException("The trip has not started.", 409, "TRIP_NOT_STARTED");

        var existingLog = await _context.PlaceVisitLogs
            .FirstOrDefaultAsync(x => x.TimelineEntryId == entry.Id, cancellationToken);
        if (isVisited)
        {
            if (existingLog is not null)
                return;
            var destination = trip.Destinations.First(x => x.Id == entry.DestinationId);
            var now = _dateTimeService!.NowUtc;
            var localDate = _temporalService.GetLocalDate(now, destination.Timezone!);
            if (localDate < destination.ArrivalDate || localDate > destination.DepartureDate)
                throw new ApiException("The timeline entry is outside its active destination date.", 422);
            _context.PlaceVisitLogs.Add(PlaceVisitLog.Create(
                trip.Id, destination.Id, userId, entry.Id, null, now,
                null, entry.CurrencyCode, null, null, trip.BaseCurrencyCode));
            entry.MarkVisitedAt(now);
        }
    }
}
