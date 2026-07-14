using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Currency;
using OmniFlow.Application.DTOs.VisitLogs;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.VisitLogs.Commands.CreateVisitLog;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Services;

namespace OmniFlow.Application.Features.VisitLogs.Commands.UpdateVisitLog;

public sealed record UpdateVisitLogCommand(
    Guid TripId,
    Guid VisitLogId,
    Guid? TripDestinationId,
    DateTime VisitedAt,
    decimal? ActualCost,
    string CurrencyCode,
    int? Rating,
    string? Note) : IRequest<VisitLogResponse>;

public sealed class UpdateVisitLogCommandValidator : AbstractValidator<UpdateVisitLogCommand>
{
    public UpdateVisitLogCommandValidator()
    {
        RuleFor(x => x.VisitedAt).Must(x => x.Kind == DateTimeKind.Utc);
        RuleFor(x => x.ActualCost).GreaterThanOrEqualTo(0).When(x => x.ActualCost.HasValue);
        RuleFor(x => x.CurrencyCode).Must(CurrencyPolicy.IsSupported);
        RuleFor(x => x.Rating).InclusiveBetween(1, 5).When(x => x.Rating.HasValue);
        RuleFor(x => x.Note).MaximumLength(1000);
    }
}

public sealed class UpdateVisitLogCommandHandler(
    IApplicationDbContext context,
    IAuthenticatedUserService authenticatedUserService,
    ITripTemporalService temporalService,
    IDateTimeService dateTimeService,
    IVisitLogConversionService conversionService)
    : IRequestHandler<UpdateVisitLogCommand, VisitLogResponse>
{
    public async Task<VisitLogResponse> Handle(UpdateVisitLogCommand request, CancellationToken cancellationToken)
    {
        var trip = await context.Trips.Include(x => x.Destinations)
            .FirstOrDefaultAsync(x => x.Id == request.TripId, cancellationToken)
            ?? throw new EntityNotFoundException("Trip", request.TripId);
        if (trip.OwnerId != Guid.Parse(authenticatedUserService.UserId))
            throw new ForbiddenException("You are not authorized to update visit logs for this trip.");
        if (!temporalService.GetExecutionState(trip).IsTimezoneComplete)
            throw new ApiException("Trip timezone information is incomplete.", 409, "TIMEZONE_UNAVAILABLE");

        var log = await context.PlaceVisitLogs
            .FirstOrDefaultAsync(x => x.Id == request.VisitLogId && x.TripId == request.TripId, cancellationToken)
            ?? throw new EntityNotFoundException("PlaceVisitLog", request.VisitLogId);
        if (log.UserId != Guid.Parse(authenticatedUserService.UserId))
            throw new ForbiddenException("You are not authorized to update this visit log.");
        if (request.VisitedAt > dateTimeService.NowUtc)
            throw new ApiException("visitedAt cannot be in the future.", 422);

        var destinationId = log.TimelineEntryId.HasValue
            ? log.TripDestinationId
            : request.TripDestinationId ?? log.TripDestinationId;
        var destination = await context.TripDestinations
            .FirstOrDefaultAsync(x => x.Id == destinationId && x.TripId == request.TripId, cancellationToken)
            ?? throw new EntityNotFoundException("TripDestination", destinationId);
        if (string.IsNullOrWhiteSpace(destination.Timezone))
            throw new ApiException("Destination timezone information is incomplete.", 409, "TIMEZONE_UNAVAILABLE");
        var localDate = temporalService.GetLocalDate(request.VisitedAt, destination.Timezone);
        if (localDate < destination.ArrivalDate || localDate > destination.DepartureDate)
            throw new ApiException("visitedAt is outside the destination date range.", 422);

        log.Update(
            request.VisitedAt,
            request.ActualCost,
            CurrencyPolicy.Normalize(request.CurrencyCode),
            request.Rating,
            request.Note,
            destinationId);
        if (log.TimelineEntryId.HasValue)
        {
            var entry = await context.TimelineEntries.FindAsync([log.TimelineEntryId.Value], cancellationToken);
            entry?.MarkVisitedAt(request.VisitedAt);
        }
        await context.SaveChangesAsync(cancellationToken);
        await conversionService.TryCompleteAsync(log, cancellationToken);
        return VisitLogMapper.ToResponse(log);
    }
}
