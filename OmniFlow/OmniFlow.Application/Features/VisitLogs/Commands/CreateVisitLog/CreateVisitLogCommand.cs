using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Currency;
using OmniFlow.Application.DTOs.VisitLogs;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Services;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.VisitLogs.Commands.CreateVisitLog;

public sealed class CreateVisitLogCommand : IRequest<VisitLogResponse>
{
    public Guid TripId { get; init; }
    public Guid? TimelineEntryId { get; init; }
    public Guid? PlaceId { get; init; }
    public Guid? TripDestinationId { get; init; }
    public DateTime VisitedAt { get; init; }
    public decimal? ActualCost { get; init; }
    public string? CurrencyCode { get; init; }
    public int? Rating { get; init; }
    public string? Note { get; init; }
}

public sealed class CreateVisitLogCommandValidator : AbstractValidator<CreateVisitLogCommand>
{
    public CreateVisitLogCommandValidator()
    {
        RuleFor(x => x).Must(x => x.TimelineEntryId.HasValue != x.PlaceId.HasValue)
            .WithMessage("Exactly one of timelineEntryId or placeId is required.");
        RuleFor(x => x.TripDestinationId).NotEmpty().When(x => x.PlaceId.HasValue)
            .WithMessage("tripDestinationId is required for spontaneous visits.");
        RuleFor(x => x.VisitedAt).Must(x => x.Kind == DateTimeKind.Utc)
            .WithMessage("visitedAt must be UTC.");
        RuleFor(x => x.ActualCost).GreaterThanOrEqualTo(0).When(x => x.ActualCost.HasValue);
        RuleFor(x => x.Rating).InclusiveBetween(1, 5).When(x => x.Rating.HasValue);
        RuleFor(x => x.Note).MaximumLength(1000);
        RuleFor(x => x.CurrencyCode!).Must(CurrencyPolicy.IsSupported)
            .When(x => !string.IsNullOrWhiteSpace(x.CurrencyCode));
    }
}

public sealed class CreateVisitLogCommandHandler(
    IApplicationDbContext context,
    IAuthenticatedUserService authenticatedUserService,
    ITripTemporalService temporalService,
    IDateTimeService dateTimeService,
    IVisitLogConversionService conversionService)
    : IRequestHandler<CreateVisitLogCommand, VisitLogResponse>
{
    public async Task<VisitLogResponse> Handle(CreateVisitLogCommand request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(authenticatedUserService.UserId);
        var trip = await context.Trips.Include(x => x.Destinations)
            .FirstOrDefaultAsync(x => x.Id == request.TripId, cancellationToken)
            ?? throw new EntityNotFoundException("Trip", request.TripId);
        if (trip.OwnerId != userId)
            throw new ForbiddenException("You are not authorized to access visit logs for this trip.");

        var execution = temporalService.GetExecutionState(trip);
        if (!execution.IsTimezoneComplete)
            throw new ApiException("Trip timezone information is incomplete.", 409, "TIMEZONE_UNAVAILABLE");
        if (execution.State == TripExecutionState.Upcoming)
            throw new ApiException("The trip has not started.", 409, "TRIP_NOT_STARTED");
        if (request.VisitedAt > dateTimeService.NowUtc)
            throw new ApiException("visitedAt cannot be in the future.", 422);

        var target = await ResolveTargetAsync(request, trip, cancellationToken);
        ValidateVisitedDate(request.VisitedAt, target.Destination, temporalService);
        var currency = CurrencyPolicy.Normalize(request.CurrencyCode ?? target.DefaultCurrency);
        var log = PlaceVisitLog.Create(
            trip.Id,
            target.Destination.Id,
            userId,
            request.TimelineEntryId,
            request.PlaceId,
            request.VisitedAt,
            request.ActualCost,
            currency,
            request.Rating,
            request.Note,
            trip.BaseCurrencyCode);

        context.PlaceVisitLogs.Add(log);
        if (target.TimelineEntry is not null)
            target.TimelineEntry.MarkVisitedAt(request.VisitedAt);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new ApiException("A conflicting visit log already exists.", 409);
        }
        await conversionService.TryCompleteAsync(log, cancellationToken);
        return VisitLogMapper.ToResponse(log);
    }

    private async Task<VisitTarget> ResolveTargetAsync(CreateVisitLogCommand request, Trip trip, CancellationToken cancellationToken)
    {
        if (request.TimelineEntryId.HasValue)
        {
            var entry = await context.TimelineEntries.Include(x => x.Destination)
                .FirstOrDefaultAsync(x => x.Id == request.TimelineEntryId && x.TripId == trip.Id, cancellationToken)
                ?? throw new EntityNotFoundException("TimelineEntry", request.TimelineEntryId.Value);
            if (entry.EntryType is not (TimelineEntryType.Place or TimelineEntryType.CustomEvent))
                throw new ApiException("This timeline entry type cannot be visited.", 422, "VISIT_LOG_UNSUPPORTED_ENTRY_TYPE");
            if (await context.PlaceVisitLogs.AnyAsync(x => x.TimelineEntryId == entry.Id, cancellationToken))
                throw new ApiException("A visit log already exists for this timeline entry.", 409);
            return new VisitTarget(entry.Destination!, entry.CurrencyCode, entry);
        }

        var destination = trip.Destinations.FirstOrDefault(x => x.Id == request.TripDestinationId)
            ?? throw new EntityNotFoundException("TripDestination", request.TripDestinationId!);
        var place = await context.Places.FirstOrDefaultAsync(x => x.Id == request.PlaceId, cancellationToken)
            ?? throw new EntityNotFoundException("Place", request.PlaceId!);
        return new VisitTarget(destination, place.CurrencyCode, null);
    }

    private static void ValidateVisitedDate(DateTime visitedAt, TripDestination destination, ITripTemporalService temporalService)
    {
        if (string.IsNullOrWhiteSpace(destination.Timezone))
            throw new ApiException("Destination timezone information is incomplete.", 409, "TIMEZONE_UNAVAILABLE");
        var localDate = temporalService.GetLocalDate(visitedAt, destination.Timezone);
        if (localDate < destination.ArrivalDate || localDate > destination.DepartureDate)
            throw new ApiException("visitedAt is outside the destination date range.", 422);
    }

    private sealed record VisitTarget(
        TripDestination Destination,
        string DefaultCurrency,
        TimelineEntry? TimelineEntry);
}

internal static class VisitLogMapper
{
    public static VisitLogResponse ToResponse(PlaceVisitLog log) => new()
    {
        Id = log.Id,
        TripId = log.TripId,
        TripDestinationId = log.TripDestinationId,
        TimelineEntryId = log.TimelineEntryId,
        PlaceId = log.PlaceId,
        VisitedAt = log.VisitedAt,
        ActualCost = log.ActualCost,
        CurrencyCode = log.CurrencyCode,
        Rating = log.Rating,
        Note = log.Note,
        ConvertedActualCost = log.ConvertedActualCost,
        ExchangeRate = log.ExchangeRate,
        RateRequestedDate = log.RateRequestedDate,
        ExchangeRateDate = log.ExchangeRateDate,
        BaseCurrencyCode = log.BaseCurrencyCode,
        ConversionStatus = log.ConversionStatus
    };
}
