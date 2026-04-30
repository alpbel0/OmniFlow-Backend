using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.DTOs.TimelineEntries;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.TimelineEntries.Commands.CreateTimelineEntry;

public class CreateTimelineEntryCommandHandler : IRequestHandler<CreateTimelineEntryCommand, TimelineEntryResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ITimelineEntryRepositoryAsync _timelineRepo;
    private readonly ITimelineService _timelineService;
    private readonly IAuthenticatedUserService _authService;
    private readonly IMapper _mapper;

    public CreateTimelineEntryCommandHandler(
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

    public async Task<TimelineEntryResponse> Handle(CreateTimelineEntryCommand request, CancellationToken cancellationToken)
    {
        // 1. Load trip
        var trip = await _context.Trips
            .FirstOrDefaultAsync(t => t.Id == request.TripId && t.DeletedAt == null, cancellationToken)
            ?? throw new EntityNotFoundException("Trip", request.TripId);

        // 2. Ownership check
        var currentUserId = Guid.Parse(_authService.UserId);
        if (trip.OwnerId != currentUserId)
            throw new ForbiddenException("You are not authorized to modify this trip.");

        // 3. Draft-only
        if (trip.Status != TripStatus.Draft)
            throw new ApiException("Only draft trips can be modified.");

        // 4. Destination valid?
        var destination = trip.Destinations.FirstOrDefault(d => d.Id == request.DestinationId && d.DeletedAt == null)
            ?? throw new EntityNotFoundException("TripDestination", request.DestinationId);

        // 5. Factory by EntryType
        TimelineEntry entry = CreateEntryFromRequest(request);

        // 6. LexoRank
        var lastEntry = await _timelineRepo.GetLastEntryInDayAsync(request.TripId, request.DestinationId, request.DayNumber);
        var prevIndex = lastEntry?.OrderIndex;
        entry.OrderIndex = _timelineService.GetLexoRankBetween(prevIndex, null);

        // 7. Validate capacity + conflict
        var dayEntries = await _timelineRepo.GetByTripAndDayAsync(request.TripId, request.DestinationId, request.DayNumber);
        var validation = _timelineService.ValidateNewEntry(entry, dayEntries, trip.Tempo, destination.ArrivalDate);
        if (!validation.IsValid)
            throw new ApiException(validation.ErrorMessage ?? "Timeline validation failed.");

        // 8. Save
        await _timelineRepo.AddAsync(entry);

        // Eager-load Place for response mapping
        if (entry.PlaceId.HasValue)
        {
            entry = await _timelineRepo.GetByIdWithPlaceAsync(entry.Id)
                ?? entry;
        }

        return _mapper.Map<TimelineEntryResponse>(entry);
    }

    private static TimelineEntry CreateEntryFromRequest(CreateTimelineEntryCommand request)
    {
        return request.EntryType switch
        {
            TimelineEntryType.Place => TimelineEntry.CreatePlaceEntry(
                request.TripId, request.DestinationId, request.DayNumber, 0,
                request.PlaceId ?? throw new ApiException("PlaceId is required for Place entries.")),

            TimelineEntryType.CustomFlight => TimelineEntry.CreateCustomFlightEntry(
                request.TripId, request.DestinationId, request.DayNumber, 0,
                request.FlightFromAirport ?? string.Empty,
                request.FlightToAirport ?? string.Empty,
                request.FlightDepartureAt ?? throw new ApiException("FlightDepartureAt is required for CustomFlight entries."),
                request.FlightArrivalAt ?? throw new ApiException("FlightArrivalAt is required for CustomFlight entries."),
                request.FlightFromCity,
                request.FlightToCity,
                request.Airline,
                request.FlightNumber,
                request.Price,
                request.Notes),

            TimelineEntryType.CustomTransport => TimelineEntry.CreateCustomTransportEntry(
                request.TripId, request.DestinationId, request.DayNumber, 0,
                request.TransportType ?? throw new ApiException("TransportType is required for CustomTransport entries."),
                request.StartTime,
                request.DurationMinutes,
                request.TransportFromStation,
                request.TransportToStation,
                request.TransportCompany,
                request.Price,
                request.Notes),

            TimelineEntryType.CustomAccommodation => TimelineEntry.CreateCustomAccommodationEntry(
                request.TripId, request.DestinationId, request.DayNumber, 0,
                request.AccommodationCheckIn ?? throw new ApiException("AccommodationCheckIn is required for CustomAccommodation entries."),
                request.AccommodationCheckOut ?? throw new ApiException("AccommodationCheckOut is required for CustomAccommodation entries."),
                request.CustomName ?? throw new ApiException("CustomName is required for CustomAccommodation entries."),
                request.AccommodationAddress,
                request.Price,
                request.Notes),

            TimelineEntryType.CustomEvent => TimelineEntry.CreateCustomEventEntry(
                request.TripId, request.DestinationId, request.DayNumber, 0,
                request.CustomName ?? throw new ApiException("CustomName is required for CustomEvent entries."),
                request.StartTime ?? throw new ApiException("StartTime is required for CustomEvent entries."),
                request.DurationMinutes ?? throw new ApiException("DurationMinutes is required for CustomEvent entries."),
                request.CustomCategory,
                request.Price,
                request.Notes),

            _ => throw new ApiException($"Unsupported EntryType: {request.EntryType}")
        };
    }
}
