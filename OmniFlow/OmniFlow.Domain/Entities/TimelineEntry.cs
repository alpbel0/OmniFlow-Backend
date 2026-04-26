using OmniFlow.Domain.Common;
using OmniFlow.Domain.Enums;
using OmniFlow.Domain.Exceptions;

namespace OmniFlow.Domain.Entities;

public class TimelineEntry : AuditableBaseEntity
{
    // === Core ===
    public Guid TripId { get; private set; }
    public Guid DestinationId { get; private set; }
    public int DayNumber { get; private set; }
    public TimelineEntryType EntryType { get; private set; }

    // === Ordering (public setter for reorder service) ===
    public double OrderIndex { get; set; }

    // === Place (required when EntryType = Place) ===
    public Guid? PlaceId { get; private set; }

    // === Custom common ===
    public string? CustomName { get; set; }
    public PlaceCategory? CustomCategory { get; set; }
    public string? CustomPhotoUrl { get; set; }
    public double? CustomLatitude { get; set; }
    public double? CustomLongitude { get; set; }
    public string? CustomDescription { get; set; }

    // === Timing & Locking ===
    public TimeOnly? StartTime { get; set; }
    public int? DurationMinutes { get; set; }
    public bool IsLocked { get; private set; }
    public int? BufferMinutes { get; private set; }

    // === CustomFlight specific ===
    public string? FlightFromAirport { get; private set; }
    public string? FlightToAirport { get; private set; }
    public string? FlightFromCity { get; set; }
    public string? FlightToCity { get; set; }
    public DateTime? FlightDepartureAt { get; private set; }
    public DateTime? FlightArrivalAt { get; private set; }
    public string? Airline { get; set; }
    public string? FlightNumber { get; set; }

    // === CustomTransport specific ===
    public TransportMode? TransportType { get; private set; }
    public string? TransportFromStation { get; set; }
    public string? TransportToStation { get; set; }
    public string? TransportCompany { get; set; }

    // === CustomAccommodation specific ===
    public DateTime? AccommodationCheckIn { get; private set; }
    public DateTime? AccommodationCheckOut { get; private set; }
    public string? AccommodationAddress { get; set; }

    // === Pricing ===
    public decimal Price { get; set; } = 0;
    public string CurrencyCode { get; set; } = "USD";

    // === Provider references ===
    public Guid? ProviderFlightId { get; set; }
    public Guid? ProviderHotelId { get; set; }

    // === Extra info ===
    public string? Notes { get; set; }
    public bool IsVisited { get; set; }
    public DateTime? VisitedAt { get; private set; }
    public StopAddedBy AddedBy { get; set; } = StopAddedBy.User;
    public string? AiReasoning { get; set; }

    // === Navigations ===
    public Trip? Trip { get; set; }
    public TripDestination? Destination { get; set; }
    public Place? Place { get; set; }
    public ProviderFlight? ProviderFlight { get; set; }
    public ProviderHotel? ProviderHotel { get; set; }

    // EF Core parameterless constructor
    private TimelineEntry()
    {
    }

    // ------------------------------------------------------------------
    // Factory: Place (from DB)
    // ------------------------------------------------------------------
    public static TimelineEntry CreatePlaceEntry(
        Guid tripId,
        Guid destinationId,
        int dayNumber,
        double orderIndex,
        Guid placeId)
    {
        if (placeId == Guid.Empty)
            throw new DomainException("PlaceId is required for a Place entry.");

        return new TimelineEntry
        {
            TripId = tripId,
            DestinationId = destinationId,
            DayNumber = dayNumber,
            OrderIndex = orderIndex,
            EntryType = TimelineEntryType.Place,
            PlaceId = placeId,
            IsLocked = false,
            BufferMinutes = null
        };
    }

    // ------------------------------------------------------------------
    // Factory: Custom Flight
    // ------------------------------------------------------------------
    public static TimelineEntry CreateCustomFlightEntry(
        Guid tripId,
        Guid destinationId,
        int dayNumber,
        double orderIndex,
        string fromAirport,
        string toAirport,
        DateTime departureAt,
        DateTime arrivalAt,
        string? fromCity = null,
        string? toCity = null,
        string? airline = null,
        string? flightNumber = null,
        decimal price = 0,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(fromAirport))
            throw new DomainException("FlightFromAirport is required for a CustomFlight entry.");
        if (string.IsNullOrWhiteSpace(toAirport))
            throw new DomainException("FlightToAirport is required for a CustomFlight entry.");
        if (departureAt == default)
            throw new DomainException("FlightDepartureAt is required for a CustomFlight entry.");
        if (arrivalAt == default)
            throw new DomainException("FlightArrivalAt is required for a CustomFlight entry.");
        if (arrivalAt <= departureAt)
            throw new DomainException("FlightArrivalAt must be after FlightDepartureAt.");

        return new TimelineEntry
        {
            TripId = tripId,
            DestinationId = destinationId,
            DayNumber = dayNumber,
            OrderIndex = orderIndex,
            EntryType = TimelineEntryType.CustomFlight,
            FlightFromAirport = fromAirport.Trim(),
            FlightToAirport = toAirport.Trim(),
            FlightFromCity = fromCity?.Trim(),
            FlightToCity = toCity?.Trim(),
            FlightDepartureAt = departureAt,
            FlightArrivalAt = arrivalAt,
            Airline = airline?.Trim(),
            FlightNumber = flightNumber?.Trim(),
            IsLocked = true,
            BufferMinutes = 120,
            Price = price,
            Notes = notes
        };
    }

    // ------------------------------------------------------------------
    // Factory: Custom Transport
    // ------------------------------------------------------------------
    public static TimelineEntry CreateCustomTransportEntry(
        Guid tripId,
        Guid destinationId,
        int dayNumber,
        double orderIndex,
        TransportMode transportType,
        string? fromStation = null,
        string? toStation = null,
        string? company = null,
        decimal price = 0,
        string? notes = null)
    {
        if (transportType == default)
            throw new DomainException("TransportType is required for a CustomTransport entry.");

        return new TimelineEntry
        {
            TripId = tripId,
            DestinationId = destinationId,
            DayNumber = dayNumber,
            OrderIndex = orderIndex,
            EntryType = TimelineEntryType.CustomTransport,
            TransportType = transportType,
            TransportFromStation = fromStation?.Trim(),
            TransportToStation = toStation?.Trim(),
            TransportCompany = company?.Trim(),
            IsLocked = true,
            BufferMinutes = 30,
            Price = price,
            Notes = notes
        };
    }

    // ------------------------------------------------------------------
    // Factory: Custom Accommodation
    // ------------------------------------------------------------------
    public static TimelineEntry CreateCustomAccommodationEntry(
        Guid tripId,
        Guid destinationId,
        int dayNumber,
        double orderIndex,
        DateTime checkIn,
        DateTime checkOut,
        string name,
        string? address = null,
        decimal price = 0,
        string? notes = null)
    {
        if (checkIn == default)
            throw new DomainException("AccommodationCheckIn is required for a CustomAccommodation entry.");
        if (checkOut == default)
            throw new DomainException("AccommodationCheckOut is required for a CustomAccommodation entry.");
        if (checkOut <= checkIn)
            throw new DomainException("AccommodationCheckOut must be after AccommodationCheckIn.");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("CustomName (accommodation name) is required for a CustomAccommodation entry.");

        return new TimelineEntry
        {
            TripId = tripId,
            DestinationId = destinationId,
            DayNumber = dayNumber,
            OrderIndex = orderIndex,
            EntryType = TimelineEntryType.CustomAccommodation,
            CustomName = name.Trim(),
            AccommodationCheckIn = checkIn,
            AccommodationCheckOut = checkOut,
            AccommodationAddress = address?.Trim(),
            IsLocked = true,
            BufferMinutes = 0,
            Price = price,
            Notes = notes
        };
    }

    // ------------------------------------------------------------------
    // Factory: Custom Event
    // ------------------------------------------------------------------
    public static TimelineEntry CreateCustomEventEntry(
        Guid tripId,
        Guid destinationId,
        int dayNumber,
        double orderIndex,
        string name,
        TimeOnly startTime,
        int durationMinutes,
        PlaceCategory? category = null,
        decimal price = 0,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("CustomName is required for a CustomEvent entry.");
        if (durationMinutes <= 0)
            throw new DomainException("DurationMinutes must be greater than 0 for a CustomEvent entry.");

        return new TimelineEntry
        {
            TripId = tripId,
            DestinationId = destinationId,
            DayNumber = dayNumber,
            OrderIndex = orderIndex,
            EntryType = TimelineEntryType.CustomEvent,
            CustomName = name.Trim(),
            CustomCategory = category,
            StartTime = startTime,
            DurationMinutes = durationMinutes,
            IsLocked = true,
            BufferMinutes = 0,
            Price = price,
            Notes = notes
        };
    }

    // ------------------------------------------------------------------
    // Domain methods
    // ------------------------------------------------------------------
    public void MarkVisited()
    {
        IsVisited = true;
        VisitedAt = DateTime.UtcNow;
    }

    public void MarkUnvisited()
    {
        IsVisited = false;
        VisitedAt = null;
    }

    /// <summary>
    /// Creates a deep copy of this entry for a forked trip.
    /// </summary>
    public TimelineEntry CloneForFork(Guid newTripId, Guid newDestinationId)
    {
        var clone = new TimelineEntry
        {
            Id = Guid.NewGuid(),
            TripId = newTripId,
            DestinationId = newDestinationId,
            DayNumber = DayNumber,
            OrderIndex = OrderIndex,
            EntryType = EntryType,
            PlaceId = PlaceId,
            CustomName = CustomName,
            CustomCategory = CustomCategory,
            CustomPhotoUrl = CustomPhotoUrl,
            CustomLatitude = CustomLatitude,
            CustomLongitude = CustomLongitude,
            CustomDescription = CustomDescription,
            StartTime = StartTime,
            DurationMinutes = DurationMinutes,
            IsLocked = IsLocked,
            BufferMinutes = BufferMinutes,
            FlightFromAirport = FlightFromAirport,
            FlightToAirport = FlightToAirport,
            FlightFromCity = FlightFromCity,
            FlightToCity = FlightToCity,
            FlightDepartureAt = FlightDepartureAt,
            FlightArrivalAt = FlightArrivalAt,
            Airline = Airline,
            FlightNumber = FlightNumber,
            TransportType = TransportType,
            TransportFromStation = TransportFromStation,
            TransportToStation = TransportToStation,
            TransportCompany = TransportCompany,
            AccommodationCheckIn = AccommodationCheckIn,
            AccommodationCheckOut = AccommodationCheckOut,
            AccommodationAddress = AccommodationAddress,
            Price = Price,
            CurrencyCode = CurrencyCode,
            ProviderFlightId = ProviderFlightId,
            ProviderHotelId = ProviderHotelId,
            Notes = Notes,
            IsVisited = false,
            AddedBy = AddedBy,
            AiReasoning = AiReasoning
        };
        clone.MarkUnvisited();
        return clone;
    }
}
