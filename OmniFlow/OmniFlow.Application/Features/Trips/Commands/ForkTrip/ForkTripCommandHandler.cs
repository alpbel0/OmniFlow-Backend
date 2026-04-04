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
        // 1. Get original trip with all related data (Stops, Flights, Hotels, Owner)
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
            City = originalTrip.City,
            Country = originalTrip.Country,
            StartDate = originalTrip.StartDate,
            EndDate = originalTrip.EndDate,
            PersonCount = originalTrip.PersonCount,
            BudgetTier = originalTrip.BudgetTier,
            TravelStyle = originalTrip.TravelStyle,
            UserBudget = originalTrip.UserBudget,
            EstimatedCost = originalTrip.EstimatedCost,
            ForkCount = 0,
            UpvoteCount = 0,
            ViewCount = 0,
            PopularityScore = 0,
            Tags = originalTrip.Tags.ToList()
        };

        // 7. Deep copy Stops (new IDs, reset visited state)
        foreach (var originalStop in originalTrip.Stops)
        {
            var forkedStop = new Stop
            {
                TripId = forkedTrip.Id,
                PlaceId = originalStop.PlaceId,
                FallbackPlaceId = originalStop.FallbackPlaceId,
                DayNumber = originalStop.DayNumber,
                OrderIndex = originalStop.OrderIndex,
                ArrivalTime = originalStop.ArrivalTime,
                DurationMinutes = originalStop.DurationMinutes,
                IsTimeLocked = originalStop.IsTimeLocked,
                CustomName = originalStop.CustomName,
                CustomCategory = originalStop.CustomCategory,
                CustomPhotoUrl = originalStop.CustomPhotoUrl,
                CustomLatitude = originalStop.CustomLatitude,
                CustomLongitude = originalStop.CustomLongitude,
                Notes = originalStop.Notes,
                BookingReference = null,
                ReservationNote = null,
                ActivityPrice = originalStop.ActivityPrice,
                TransportPrice = originalStop.TransportPrice,
                CurrencyCode = originalStop.CurrencyCode,
                TransportFromPrevious = originalStop.TransportFromPrevious,
                TravelTimeFromPrevious = originalStop.TravelTimeFromPrevious,
                IsVisited = false,
                VisitedAt = null,
                AddedBy = originalStop.AddedBy,
                AiReasoning = originalStop.AiReasoning
            };
            forkedTrip.Stops.Add(forkedStop);
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