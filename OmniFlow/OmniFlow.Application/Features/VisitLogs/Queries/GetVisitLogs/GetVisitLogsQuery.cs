using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.DTOs.VisitLogs;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.VisitLogs.Commands.CreateVisitLog;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Wrappers;

namespace OmniFlow.Application.Features.VisitLogs.Queries.GetVisitLogs;

public sealed record GetVisitLogsQuery(
    Guid TripId,
    int PageNumber,
    int PageSize,
    Guid? TripDestinationId,
    string? Source,
    DateTime? VisitedFrom,
    DateTime? VisitedTo,
    string Sort) : IRequest<PagedResponse<VisitLogResponse>>;

public sealed class GetVisitLogsQueryValidator : AbstractValidator<GetVisitLogsQuery>
{
    public GetVisitLogsQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Source).Must(x => x is null or "planned" or "spontaneous");
        RuleFor(x => x.Sort).Must(x => x is "visitedAtDesc" or "visitedAtAsc");
        RuleFor(x => x.VisitedFrom).Must(x => !x.HasValue || x.Value.Kind == DateTimeKind.Utc);
        RuleFor(x => x.VisitedTo).Must(x => !x.HasValue || x.Value.Kind == DateTimeKind.Utc);
    }
}

public sealed class GetVisitLogsQueryHandler(
    IApplicationDbContext context,
    IAuthenticatedUserService authenticatedUserService,
    ITripTemporalService temporalService)
    : IRequestHandler<GetVisitLogsQuery, PagedResponse<VisitLogResponse>>
{
    public async Task<PagedResponse<VisitLogResponse>> Handle(GetVisitLogsQuery request, CancellationToken cancellationToken)
    {
        var trip = await context.Trips.Include(x => x.Destinations)
            .FirstOrDefaultAsync(x => x.Id == request.TripId, cancellationToken)
            ?? throw new EntityNotFoundException("Trip", request.TripId);
        if (trip.OwnerId != Guid.Parse(authenticatedUserService.UserId))
            throw new ForbiddenException("You are not authorized to access visit logs for this trip.");
        if (!temporalService.GetExecutionState(trip).IsTimezoneComplete)
            throw new ApiException("Trip timezone information is incomplete.", 409, "TIMEZONE_UNAVAILABLE");
        if (request.TripDestinationId.HasValue &&
            !await context.TripDestinations.AnyAsync(x => x.Id == request.TripDestinationId && x.TripId == trip.Id, cancellationToken))
            throw new EntityNotFoundException("TripDestination", request.TripDestinationId.Value);

        var query = context.PlaceVisitLogs.Where(x => x.TripId == request.TripId);
        if (request.TripDestinationId.HasValue)
            query = query.Where(x => x.TripDestinationId == request.TripDestinationId);
        if (request.Source == "planned")
            query = query.Where(x => x.TimelineEntryId != null);
        else if (request.Source == "spontaneous")
            query = query.Where(x => x.PlaceId != null);
        if (request.VisitedFrom.HasValue)
            query = query.Where(x => x.VisitedAt >= request.VisitedFrom);
        if (request.VisitedTo.HasValue)
            query = query.Where(x => x.VisitedAt <= request.VisitedTo);

        var total = await query.CountAsync(cancellationToken);
        query = request.Sort == "visitedAtAsc"
            ? query.OrderBy(x => x.VisitedAt).ThenBy(x => x.Id)
            : query.OrderByDescending(x => x.VisitedAt).ThenByDescending(x => x.Id);
        var logs = await query.Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);
        return new PagedResponse<VisitLogResponse>(
            logs.Select(VisitLogMapper.ToResponse).ToList(),
            request.PageNumber,
            request.PageSize,
            total);
    }
}
