using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;
using OmniFlow.Domain.Exceptions;

namespace OmniFlow.Application.Features.Trips.Commands.ForkTrip;

public class ForkTripCommandHandler : IRequestHandler<ForkTripCommand, Guid>
{
    private readonly ITripRepositoryAsync _tripRepository;
    private readonly IApplicationDbContext _context;
    private readonly IAuthenticatedUserService _authenticatedUserService;
    private readonly IKarmaService _karmaService;
    private readonly INotificationService _notificationService;

    public ForkTripCommandHandler(
        ITripRepositoryAsync tripRepository,
        IApplicationDbContext context,
        IAuthenticatedUserService authenticatedUserService,
        IKarmaService karmaService,
        INotificationService notificationService)
    {
        _tripRepository = tripRepository;
        _context = context;
        _authenticatedUserService = authenticatedUserService;
        _karmaService = karmaService;
        _notificationService = notificationService;
    }

    public async Task<Guid> Handle(ForkTripCommand request, CancellationToken cancellationToken)
    {
        // 1. Get original trip with related data (Flights, Hotels, Owner)
        var originalTrip = await _tripRepository.GetWithAllRelatedDataAsync(request.TripId);

        // 2. Validate trip exists
        if (originalTrip == null)
            throw new EntityNotFoundException("Trip", request.TripId);

        // 3. Validate trip is Published
        if (originalTrip.Status != TripStatus.Published)
            throw new ApiException("Only published trips can be forked.", 400);

        // 4. Get current user ID
        var currentUserId = Guid.Parse(_authenticatedUserService.UserId);

        // 5. Self-fork prevention
        if (originalTrip.OwnerId == currentUserId)
            throw new SelfForkException(currentUserId, request.TripId);

        // 6. Create new Trip (deep copy with reset counters)
        var forkedTrip = new Trip
        {
            OwnerId = currentUserId,
            ForkedFromId = originalTrip.Id,
            Title = originalTrip.Title,
            Description = originalTrip.Description,
            CoverPhotoUrl = originalTrip.CoverPhotoUrl,
            Status = TripStatus.Draft,
            Origin = originalTrip.Origin,
            OriginCountry = originalTrip.OriginCountry,
            PersonCount = originalTrip.PersonCount,
            BudgetTier = originalTrip.BudgetTier,
            TravelCompanion = originalTrip.TravelCompanion,
            TravelStyles = originalTrip.TravelStyles.ToList(),
            Tempo = originalTrip.Tempo,
            TransportPreference = originalTrip.TransportPreference,
            ManualBudget = originalTrip.ManualBudget,
            AdjustedBudgetTier = originalTrip.AdjustedBudgetTier,
            EstimatedCost = originalTrip.EstimatedCost,
            BaseCurrencyCode = originalTrip.BaseCurrencyCode,
            ForkCount = 0,
            UpvoteCount = 0,
            ViewCount = 0,
            PopularityScore = 0,
            Tags = originalTrip.Tags.ToList()
        };

        // 7. Deep copy TripDestinations + TimelineEntries
        var originalDestinations = await _context.TripDestinations
            .Where(d => d.TripId == originalTrip.Id && d.DeletedAt == null)
            .ToListAsync(cancellationToken);

        var originalTimelineEntries = await _context.TimelineEntries
            .Where(e => e.TripId == originalTrip.Id && e.DeletedAt == null)
            .ToListAsync(cancellationToken);

        var destinationIdMap = new Dictionary<Guid, Guid>();
        foreach (var originalDest in originalDestinations)
        {
            var newDestId = Guid.NewGuid();
            destinationIdMap[originalDest.Id] = newDestId;

            var forkedDest = new TripDestination(
                originalDest.ArrivalDate,
                originalDest.DepartureDate,
                originalDest.City,
                originalDest.Country,
                originalDest.OrderIndex)
            {
                Id = newDestId,
                TripId = forkedTrip.Id
            };
            forkedDest.SetCoordinates(originalDest.Latitude, originalDest.Longitude);
            forkedDest.Timezone = originalDest.Timezone;
            await _context.TripDestinations.AddAsync(forkedDest, cancellationToken);
        }

        foreach (var originalEntry in originalTimelineEntries)
        {
            var newDestId = destinationIdMap[originalEntry.DestinationId];
            var forkedEntry = originalEntry.CloneForFork(forkedTrip.Id, newDestId);
            await _context.TimelineEntries.AddAsync(forkedEntry, cancellationToken);
        }

        // 8. Deep copy Flights (new IDs, reset booking state)
        var originalFlights = await _context.Flights
            .Where(f => f.TripId == originalTrip.Id)
            .ToListAsync(cancellationToken);

        foreach (var originalFlight in originalFlights)
        {
            var forkedFlight = new Flight
            {
                TripId = forkedTrip.Id,
                ItineraryGroupId = originalFlight.ItineraryGroupId,
                FlightDirection = originalFlight.FlightDirection,
                FromCity = originalFlight.FromCity,
                FromAirport = originalFlight.FromAirport,
                ToCity = originalFlight.ToCity,
                ToAirport = originalFlight.ToAirport,
                DepartureAt = originalFlight.DepartureAt,
                ArrivalAt = originalFlight.ArrivalAt,
                DurationMinutes = originalFlight.DurationMinutes,
                Airline = originalFlight.Airline,
                FlightNumber = originalFlight.FlightNumber,
                CabinClass = originalFlight.CabinClass,
                IsDirect = originalFlight.IsDirect,
                PricePerPerson = originalFlight.PricePerPerson,
                TotalPrice = originalFlight.TotalPrice,
                CurrencyCode = originalFlight.CurrencyCode,
                IsBooked = false,
                BookedAt = null,
                BookingReference = null,
                Status = originalFlight.Status,
                DataSource = originalFlight.DataSource,
                DataFetchedAt = originalFlight.DataFetchedAt
            };
            _context.Flights.Add(forkedFlight);
        }

        // 9. Deep copy Hotels (new IDs, reset booking state)
        var originalHotels = await _context.Hotels
            .Where(h => h.TripId == originalTrip.Id)
            .ToListAsync(cancellationToken);

        foreach (var originalHotel in originalHotels)
        {
            var forkedHotel = new Hotel
            {
                TripId = forkedTrip.Id,
                PlaceId = originalHotel.PlaceId,
                HotelName = originalHotel.HotelName,
                HotelLatitude = originalHotel.HotelLatitude,
                HotelLongitude = originalHotel.HotelLongitude,
                HotelAddress = originalHotel.HotelAddress,
                HotelPhone = originalHotel.HotelPhone,
                ProviderUrl = originalHotel.ProviderUrl,
                Stars = originalHotel.Stars,
                RoomType = originalHotel.RoomType,
                BreakfastIncluded = originalHotel.BreakfastIncluded,
                CancellationPolicy = originalHotel.CancellationPolicy,
                CheckIn = originalHotel.CheckIn,
                CheckOut = originalHotel.CheckOut,
                PricePerNight = originalHotel.PricePerNight,
                TotalPrice = originalHotel.TotalPrice,
                CurrencyCode = originalHotel.CurrencyCode,
                IsBooked = false,
                BookedAt = null,
                BookingReference = null,
                Status = originalHotel.Status,
                DataSource = originalHotel.DataSource,
                DataFetchedAt = originalHotel.DataFetchedAt
            };
            _context.Hotels.Add(forkedHotel);
        }

        // 10. Add forked trip to context (NOT save yet)
        await _context.Trips.AddAsync(forkedTrip, cancellationToken);

        // 11. Increment original trip's ForkCount in context
        originalTrip.ForkCount++;

        // 12. CRITICAL: Single SaveChangesAsync for atomic operation
        await _context.SaveChangesAsync(cancellationToken);

        await _karmaService.AwardKarmaAsync(
            originalTrip.OwnerId,
            currentUserId,
            KarmaEventType.TripForked,
            5,
            originalTrip.Id,
            KarmaSourceType.Trip);

        await _notificationService.CreateNotificationAsync(
            originalTrip.OwnerId,
            currentUserId,
            NotificationType.Fork,
            originalTrip.Id,
            NotificationTargetType.Trip);

        // 14. Return new trip ID
        return forkedTrip.Id;
    }
}
