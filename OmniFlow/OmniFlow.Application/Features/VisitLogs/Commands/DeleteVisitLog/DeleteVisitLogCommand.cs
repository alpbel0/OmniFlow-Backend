using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Application.Features.VisitLogs.Commands.DeleteVisitLog;

public sealed record DeleteVisitLogCommand(Guid TripId, Guid VisitLogId) : IRequest<Unit>;

public sealed class DeleteVisitLogCommandHandler(
    IApplicationDbContext context,
    IAuthenticatedUserService authenticatedUserService,
    IDateTimeService dateTimeService,
    ITripTemporalService temporalService) : IRequestHandler<DeleteVisitLogCommand, Unit>
{
    public async Task<Unit> Handle(DeleteVisitLogCommand request, CancellationToken cancellationToken)
    {
        var trip = await context.Trips.Include(x => x.Destinations)
            .FirstOrDefaultAsync(x => x.Id == request.TripId, cancellationToken)
            ?? throw new EntityNotFoundException("Trip", request.TripId);
        if (trip.OwnerId != Guid.Parse(authenticatedUserService.UserId))
            throw new ForbiddenException("You are not authorized to delete visit logs for this trip.");
        if (!temporalService.GetExecutionState(trip).IsTimezoneComplete)
            throw new ApiException("Trip timezone information is incomplete.", 409, "TIMEZONE_UNAVAILABLE");

        var log = await context.PlaceVisitLogs
            .FirstOrDefaultAsync(x => x.Id == request.VisitLogId && x.TripId == request.TripId, cancellationToken)
            ?? throw new EntityNotFoundException("PlaceVisitLog", request.VisitLogId);
        if (log.UserId != Guid.Parse(authenticatedUserService.UserId))
            throw new ForbiddenException("You are not authorized to delete this visit log.");

        log.DeletedAt = dateTimeService.NowUtc;
        if (log.TimelineEntryId.HasValue)
        {
            var entry = await context.TimelineEntries.FindAsync([log.TimelineEntryId.Value], cancellationToken);
            entry?.MarkUnvisited();
        }
        await context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
