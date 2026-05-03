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
    private readonly IProviderFlightRepositoryAsync _providerFlightRepo;
    private readonly IProviderHotelRepositoryAsync _providerHotelRepo;
    private readonly IAuthenticatedUserService _authService;
    private readonly IMapper _mapper;

    public CreateTimelineEntryCommandHandler(
        IApplicationDbContext context,
        ITimelineEntryRepositoryAsync timelineRepo,
        ITimelineService timelineService,
        IProviderFlightRepositoryAsync providerFlightRepo,
        IProviderHotelRepositoryAsync providerHotelRepo,
        IAuthenticatedUserService authService,
        IMapper mapper)
    {
        _context = context;
        _timelineRepo = timelineRepo;
        _timelineService = timelineService;
        _providerFlightRepo = providerFlightRepo;
        _providerHotelRepo = providerHotelRepo;
        _authService = authService;
        _mapper = mapper;
    }

    public async Task<TimelineEntryResponse> Handle(CreateTimelineEntryCommand request, CancellationToken cancellationToken)
    {
        // 1. Load trip with destinations
        var trip = await _context.Trips
            .Include(t => t.Destinations)
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
        TimelineEntry entry = await CreateEntryFromRequestAsync(request, trip, destination);

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

    private async Task<TimelineEntry> CreateEntryFromRequestAsync(
        CreateTimelineEntryCommand request,
        Trip trip,
        TripDestination destination)
    {
        if (request.ProviderFlightId.HasValue)
        {
            var providerFlight = await _providerFlightRepo.GetByIdAsync(request.ProviderFlightId.Value)
                ?? throw new EntityNotFoundException("ProviderFlight", request.ProviderFlightId.Value);

            var providerFlightEntry = TimelineEntry.CreateCustomFlightEntry(
                request.TripId,
                request.DestinationId,
                request.DayNumber,
                0,
                providerFlight.DepartureAirportCode,
                providerFlight.ArrivalAirportCode,
                EnsureUtc(providerFlight.DepartureTime),
                EnsureUtc(providerFlight.ArrivalTime),
                providerFlight.DepartureCity,
                providerFlight.ArrivalCity,
                providerFlight.Airline,
                providerFlight.FlightNumber,
                providerFlight.Price,
                request.Notes);

            providerFlightEntry.UpdateCommonFields(
                providerFlight.Price,
                providerFlight.CurrencyCode,
                request.Notes,
                providerFlight.Id,
                null);

            return providerFlightEntry;
        }

        if (request.ProviderHotelId.HasValue)
        {
            var providerHotel = await _providerHotelRepo.GetByIdAsync(request.ProviderHotelId.Value)
                ?? throw new EntityNotFoundException("ProviderHotel", request.ProviderHotelId.Value);

            var stayOffset = Math.Max(request.DayNumber - 1, 0);
            var nightlyCheckInDate = destination.ArrivalDate.AddDays(stayOffset);
            var nightlyCheckOutDate = nightlyCheckInDate.AddDays(1);
            if (nightlyCheckOutDate > destination.DepartureDate)
            {
                nightlyCheckOutDate = destination.DepartureDate;
            }

            if (nightlyCheckOutDate <= nightlyCheckInDate)
            {
                nightlyCheckOutDate = nightlyCheckInDate.AddDays(1);
            }

            var checkIn = EnsureUtc(nightlyCheckInDate.ToDateTime(new TimeOnly(14, 0)));
            var checkOut = EnsureUtc(nightlyCheckOutDate.ToDateTime(new TimeOnly(12, 0)));
            var personCount = trip.PersonCount < 1 ? 1 : trip.PersonCount;
            var totalPrice = providerHotel.PricePerNight * personCount;

            var providerHotelEntry = TimelineEntry.CreateCustomAccommodationEntry(
                request.TripId,
                request.DestinationId,
                request.DayNumber,
                0,
                checkIn,
                checkOut,
                providerHotel.HotelName,
                request.AccommodationAddress ?? providerHotel.City,
                providerHotel.Latitude,
                providerHotel.Longitude,
                totalPrice,
                request.Notes);

            providerHotelEntry.UpdateCommonFields(
                totalPrice,
                providerHotel.CurrencyCode,
                request.Notes,
                null,
                providerHotel.Id);

            return providerHotelEntry;
        }

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
                request.CustomLatitude,
                request.CustomLongitude,
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

    private static DateTime EnsureUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}
