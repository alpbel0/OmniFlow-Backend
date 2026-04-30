using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.DTOs.TimelineEntries;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.TimelineEntries.Commands.UpdateTimelineEntry;

public class UpdateTimelineEntryCommandHandler : IRequestHandler<UpdateTimelineEntryCommand, TimelineEntryResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ITimelineEntryRepositoryAsync _timelineRepo;
    private readonly ITimelineService _timelineService;
    private readonly IAuthenticatedUserService _authService;
    private readonly IMapper _mapper;

    public UpdateTimelineEntryCommandHandler(
        IApplicationDbContext context,
        ITimelineEntryRepositoryAsync timelineRepo,
        ITimelineService timelineService,
        IAuthenticatedUserService authService,
        IMapper mapper)
    {
        _context = context;
        _timelineRepo = timelineRepo;
        _timelineService = timelineService;
        _authService = authService;
        _mapper = mapper;
    }

    public async Task<TimelineEntryResponse> Handle(UpdateTimelineEntryCommand request, CancellationToken cancellationToken)
    {
        // 1. Load entry
        var entry = await _timelineRepo.GetByIdAsync(request.Id)
            ?? throw new EntityNotFoundException("TimelineEntry", request.Id);

        // 2. Load trip with destinations
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

        // 5. Track if destination/day changed (for conflict re-check)
        bool destinationOrDayChanged = entry.DestinationId != request.DestinationId || entry.DayNumber != request.DayNumber;

        if (destinationOrDayChanged)
        {
            var newDest = trip.Destinations.FirstOrDefault(d => d.Id == request.DestinationId && d.DeletedAt == null)
                ?? throw new EntityNotFoundException("TripDestination", request.DestinationId);

            entry.UpdateDestinationAndDay(request.DestinationId, request.DayNumber);
        }

        // 6. EntryType-specific updates
        if (entry.IsLocked)
        {
            EnsureNoTypeSpecificChanges(entry.EntryType, request);
        }
        else
        {
            ApplyTypeSpecificUpdates(entry, request);
        }

        // 7. Common fields (always allowed)
        entry.UpdateCommonFields(
            request.Price,
            request.CurrencyCode,
            request.Notes,
            request.ProviderFlightId,
            request.ProviderHotelId);

        // 8. Conflict check if time/location changed
        var targetDestinationId = entry.DestinationId;
        var targetDayNumber = entry.DayNumber;
        var targetDestination = trip.Destinations.FirstOrDefault(d => d.Id == targetDestinationId);

        if (targetDestination != null)
        {
            var dayEntries = await _timelineRepo.GetByTripAndDayAsync(trip.Id, targetDestinationId, targetDayNumber);
            // Exclude the current entry from conflict check
            var otherEntries = dayEntries.Where(e => e.Id != entry.Id).ToList();

            var validation = _timelineService.CheckConflict(entry, otherEntries, targetDestination.ArrivalDate);
            if (!validation.IsValid)
                throw new ApiException(validation.ErrorMessage ?? "Timeline conflict detected after update.");
        }

        // 9. Save
        await _context.SaveChangesAsync(cancellationToken);

        // Reload with includes for response
        entry = await _timelineRepo.GetByIdWithPlaceAsync(entry.Id) ?? entry;
        return _mapper.Map<TimelineEntryResponse>(entry);
    }

    private static void EnsureNoTypeSpecificChanges(TimelineEntryType entryType, UpdateTimelineEntryCommand request)
    {
        bool hasChanges = entryType switch
        {
            TimelineEntryType.Place =>
                request.PlaceId.HasValue || request.StartTime.HasValue || request.DurationMinutes.HasValue
                || request.CustomName != null || request.CustomCategory.HasValue || request.CustomPhotoUrl != null
                || request.CustomLatitude.HasValue || request.CustomLongitude.HasValue || request.CustomDescription != null,
            TimelineEntryType.CustomFlight =>
                request.FlightFromAirport != null || request.FlightToAirport != null
                || request.FlightFromCity != null || request.FlightToCity != null
                || request.FlightDepartureAt.HasValue || request.FlightArrivalAt.HasValue
                || request.Airline != null || request.FlightNumber != null,
            TimelineEntryType.CustomTransport =>
                request.TransportType.HasValue || request.StartTime.HasValue || request.DurationMinutes.HasValue
                || request.TransportFromStation != null || request.TransportToStation != null || request.TransportCompany != null,
            TimelineEntryType.CustomAccommodation =>
                request.AccommodationCheckIn.HasValue || request.AccommodationCheckOut.HasValue
                || request.AccommodationAddress != null || request.CustomName != null,
            TimelineEntryType.CustomEvent =>
                request.CustomName != null || request.StartTime.HasValue || request.DurationMinutes.HasValue || request.CustomCategory.HasValue
                || request.CustomPhotoUrl != null || request.CustomDescription != null
                || request.CustomLatitude.HasValue || request.CustomLongitude.HasValue,
            _ => false
        };

        if (hasChanges)
            throw new ApiException("Cannot modify time/location details of a locked timeline entry.");
    }

    private static void ApplyTypeSpecificUpdates(TimelineEntry entry, UpdateTimelineEntryCommand request)
    {
        try
        {
            switch (entry.EntryType)
            {
                case TimelineEntryType.Place:
                    entry.UpdatePlaceDetails(request.PlaceId, request.StartTime, request.DurationMinutes);
                    break;

                case TimelineEntryType.CustomFlight:
                    entry.UpdateFlightDetails(
                        request.FlightFromAirport, request.FlightToAirport,
                        request.FlightDepartureAt, request.FlightArrivalAt,
                        request.FlightFromCity, request.FlightToCity,
                        request.Airline, request.FlightNumber);
                    break;

                case TimelineEntryType.CustomTransport:
                    entry.UpdateTransportDetails(
                        request.TransportType, request.StartTime, request.DurationMinutes,
                        request.TransportFromStation, request.TransportToStation, request.TransportCompany);
                    break;

                case TimelineEntryType.CustomAccommodation:
                    entry.UpdateAccommodationDetails(
                        request.AccommodationCheckIn, request.AccommodationCheckOut,
                        request.AccommodationAddress, request.CustomName);
                    break;

                case TimelineEntryType.CustomEvent:
                    entry.UpdateEventDetails(
                        request.CustomName, request.StartTime, request.DurationMinutes, request.CustomCategory);
                    break;
            }
        }
        catch (Domain.Exceptions.DomainException ex)
        {
            throw new ApiException(ex.Message);
        }
    }
}
