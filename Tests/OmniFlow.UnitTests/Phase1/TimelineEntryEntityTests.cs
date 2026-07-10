using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;
using OmniFlow.Domain.Exceptions;

namespace OmniFlow.UnitTests.Phase1;

public class TimelineEntryEntityTests
{
	private static readonly Guid TripId = Guid.NewGuid();
	private static readonly Guid DestinationId = Guid.NewGuid();
	private static readonly Guid PlaceId = Guid.NewGuid();
	private const int DayNumber = 1;
	private const double OrderIndex = 100.0;

	// ------------------------------------------------------------------
	// CreatePlaceEntry
	// ------------------------------------------------------------------
	[Fact]
	public void CreatePlaceEntry_ValidInput_SetsProperties()
	{
		var entry = TimelineEntry.CreatePlaceEntry(TripId, DestinationId, DayNumber, OrderIndex, PlaceId);

		entry.TripId.Should().Be(TripId);
		entry.DestinationId.Should().Be(DestinationId);
		entry.DayNumber.Should().Be(DayNumber);
		entry.OrderIndex.Should().Be(OrderIndex);
		entry.EntryType.Should().Be(TimelineEntryType.Place);
		entry.PlaceId.Should().Be(PlaceId);
		entry.IsLocked.Should().BeFalse();
		entry.BufferMinutes.Should().BeNull();
		entry.PlanningSlotKey.Should().BeNull();
	}

	[Fact]
	public void SetPlanningSlotKey_ValidInput_TrimsValue()
	{
		var entry = TimelineEntry.CreatePlaceEntry(TripId, DestinationId, DayNumber, OrderIndex, PlaceId);

		entry.SetPlanningSlotKey("  hotel-night:11111111-1111-1111-1111-111111111111:1  ");

		entry.PlanningSlotKey.Should().Be("hotel-night:11111111-1111-1111-1111-111111111111:1");
	}

	[Fact]
	public void SetPlanningSlotKey_Whitespace_ClearsValue()
	{
		var entry = TimelineEntry.CreatePlaceEntry(TripId, DestinationId, DayNumber, OrderIndex, PlaceId);
		entry.SetPlanningSlotKey("hotel-night:11111111-1111-1111-1111-111111111111:1");

		entry.SetPlanningSlotKey("   ");

		entry.PlanningSlotKey.Should().BeNull();
	}

	[Fact]
	public void CloneForFork_DoesNotCopyPlanningSlotKey()
	{
		var entry = TimelineEntry.CreatePlaceEntry(TripId, DestinationId, DayNumber, OrderIndex, PlaceId);
		entry.SetPlanningSlotKey($"hotel-night:{DestinationId:D}:1");

		var clone = entry.CloneForFork(Guid.NewGuid(), Guid.NewGuid());

		clone.PlanningSlotKey.Should().BeNull();
	}

	[Fact]
	public void CreatePlaceEntry_EmptyPlaceId_Throws()
	{
		var act = () => TimelineEntry.CreatePlaceEntry(TripId, DestinationId, DayNumber, OrderIndex, Guid.Empty);

		act.Should().Throw<DomainException>()
			.WithMessage("*PlaceId is required for a Place entry*");
	}

	// ------------------------------------------------------------------
	// CreateCustomFlightEntry
	// ------------------------------------------------------------------
	[Fact]
	public void CreateCustomFlightEntry_ValidInput_SetsLockAndBuffer120()
	{
		var dep = new DateTime(2026, 8, 10, 10, 0, 0, DateTimeKind.Utc);
		var arr = new DateTime(2026, 8, 10, 14, 0, 0, DateTimeKind.Utc);

		var entry = TimelineEntry.CreateCustomFlightEntry(
			TripId, DestinationId, DayNumber, OrderIndex,
			"IST", "FCO", dep, arr,
			fromCity: "Istanbul", toCity: "Rome",
			airline: "Turkish Airlines", flightNumber: "TK1234",
			price: 450);

		entry.EntryType.Should().Be(TimelineEntryType.CustomFlight);
		entry.IsLocked.Should().BeTrue();
		entry.BufferMinutes.Should().Be(120);
		entry.FlightFromAirport.Should().Be("IST");
		entry.FlightToAirport.Should().Be("FCO");
		entry.FlightFromCity.Should().Be("Istanbul");
		entry.FlightToCity.Should().Be("Rome");
		entry.Airline.Should().Be("Turkish Airlines");
		entry.FlightNumber.Should().Be("TK1234");
		entry.Price.Should().Be(450);
	}

	[Theory]
	[InlineData(null, "FCO", "2026-08-10T10:00:00Z", "2026-08-10T14:00:00Z", "FlightFromAirport")]
	[InlineData("IST", null, "2026-08-10T10:00:00Z", "2026-08-10T14:00:00Z", "FlightToAirport")]
	public void CreateCustomFlightEntry_MissingRequired_Throws(string? from, string? to, string depStr, string arrStr, string expectedInMessage)
	{
		var dep = DateTime.Parse(depStr);
		var arr = DateTime.Parse(arrStr);

		var act = () => TimelineEntry.CreateCustomFlightEntry(
			TripId, DestinationId, DayNumber, OrderIndex,
			from!, to!, dep, arr);

		act.Should().Throw<DomainException>()
			.WithMessage($"*{expectedInMessage}*");
	}

	[Fact]
	public void CreateCustomFlightEntry_ArrivalBeforeDeparture_Throws()
	{
		var dep = new DateTime(2026, 8, 10, 14, 0, 0, DateTimeKind.Utc);
		var arr = new DateTime(2026, 8, 10, 10, 0, 0, DateTimeKind.Utc);

		var act = () => TimelineEntry.CreateCustomFlightEntry(
			TripId, DestinationId, DayNumber, OrderIndex,
			"IST", "FCO", dep, arr);

		act.Should().Throw<DomainException>()
			.WithMessage("*FlightArrivalAt must be after FlightDepartureAt*");
	}

	// ------------------------------------------------------------------
	// CreateCustomTransportEntry
	// ------------------------------------------------------------------
	[Fact]
	public void CreateCustomTransportEntry_ValidInput_SetsLockAndBuffer30()
	{
		var entry = TimelineEntry.CreateCustomTransportEntry(
			TripId, DestinationId, DayNumber, OrderIndex,
			TransportMode.Train,
			fromStation: "Roma Termini", toStation: "Firenze SMN",
			company: "Trenitalia", price: 85);

		entry.EntryType.Should().Be(TimelineEntryType.CustomTransport);
		entry.IsLocked.Should().BeTrue();
		entry.BufferMinutes.Should().Be(30);
		entry.TransportType.Should().Be(TransportMode.Train);
		entry.TransportFromStation.Should().Be("Roma Termini");
		entry.TransportToStation.Should().Be("Firenze SMN");
		entry.TransportCompany.Should().Be("Trenitalia");
		entry.Price.Should().Be(85);
	}

	[Fact]
	public void CreateCustomTransportEntry_MissingType_Throws()
	{
		var act = () => TimelineEntry.CreateCustomTransportEntry(
			TripId, DestinationId, DayNumber, OrderIndex,
			default(TransportMode));

		act.Should().Throw<DomainException>()
			.WithMessage("*TransportType is required for a CustomTransport entry*");
	}

	// ------------------------------------------------------------------
	// CreateCustomAccommodationEntry
	// ------------------------------------------------------------------
	[Fact]
	public void CreateCustomAccommodationEntry_ValidInput_SetsLockAndBuffer0()
	{
		var checkIn = new DateTime(2026, 8, 10, 14, 0, 0, DateTimeKind.Utc);
		var checkOut = new DateTime(2026, 8, 13, 12, 0, 0, DateTimeKind.Utc);

		var entry = TimelineEntry.CreateCustomAccommodationEntry(
			TripId, DestinationId, DayNumber, OrderIndex,
			checkIn, checkOut, "Hotel Artis", address: "Via Palestro 9, Rome", price: 600);

		entry.EntryType.Should().Be(TimelineEntryType.CustomAccommodation);
		entry.IsLocked.Should().BeTrue();
		entry.BufferMinutes.Should().Be(0);
		entry.CustomName.Should().Be("Hotel Artis");
		entry.AccommodationCheckIn.Should().Be(checkIn);
		entry.AccommodationCheckOut.Should().Be(checkOut);
		entry.AccommodationAddress.Should().Be("Via Palestro 9, Rome");
		entry.Price.Should().Be(600);
	}

	[Fact]
	public void CreateCustomAccommodationEntry_CheckOutBeforeCheckIn_Throws()
	{
		var checkIn = new DateTime(2026, 8, 13, 12, 0, 0, DateTimeKind.Utc);
		var checkOut = new DateTime(2026, 8, 10, 14, 0, 0, DateTimeKind.Utc);

		var act = () => TimelineEntry.CreateCustomAccommodationEntry(
			TripId, DestinationId, DayNumber, OrderIndex,
			checkIn, checkOut, "Hotel Artis");

		act.Should().Throw<DomainException>()
			.WithMessage("*AccommodationCheckOut must be after AccommodationCheckIn*");
	}

	[Fact]
	public void CreateCustomAccommodationEntry_EmptyName_Throws()
	{
		var checkIn = new DateTime(2026, 8, 10, 14, 0, 0, DateTimeKind.Utc);
		var checkOut = new DateTime(2026, 8, 13, 12, 0, 0, DateTimeKind.Utc);

		var act = () => TimelineEntry.CreateCustomAccommodationEntry(
			TripId, DestinationId, DayNumber, OrderIndex,
			checkIn, checkOut, "");

		act.Should().Throw<DomainException>()
			.WithMessage("*CustomName (accommodation name) is required*");
	}

	// ------------------------------------------------------------------
	// CreateCustomEventEntry
	// ------------------------------------------------------------------
	[Fact]
	public void CreateCustomEventEntry_ValidInput_SetsLockAndBuffer0()
	{
		var startTime = new TimeOnly(19, 30);

		var entry = TimelineEntry.CreateCustomEventEntry(
			TripId, DestinationId, DayNumber, OrderIndex,
			"Coldplay Konseri", startTime, 180,
			category: PlaceCategory.Theater, price: 120);

		entry.EntryType.Should().Be(TimelineEntryType.CustomEvent);
		entry.IsLocked.Should().BeTrue();
		entry.BufferMinutes.Should().Be(0);
		entry.CustomName.Should().Be("Coldplay Konseri");
		entry.StartTime.Should().Be(startTime);
		entry.DurationMinutes.Should().Be(180);
		entry.CustomCategory.Should().Be(PlaceCategory.Theater);
		entry.Price.Should().Be(120);
	}

	[Fact]
	public void CreateCustomEventEntry_WithCoordinates_SetsCustomLatitudeAndLongitude()
	{
		var entry = TimelineEntry.CreateCustomEventEntry(
			TripId, DestinationId, DayNumber, OrderIndex,
			"Map Point", new TimeOnly(14, 0), 60,
			customLatitude: 41.0082,
			customLongitude: 28.9784);

		entry.CustomLatitude.Should().Be(41.0082);
		entry.CustomLongitude.Should().Be(28.9784);
	}

	[Fact]
	public void CreateCustomEventEntry_WithIsLockedFalse_CreatesUnlockedEntry()
	{
		var entry = TimelineEntry.CreateCustomEventEntry(
			TripId, DestinationId, DayNumber, OrderIndex,
			"Map Place", new TimeOnly(14, 0), 60,
			isLocked: false);

		entry.IsLocked.Should().BeFalse();
	}

	[Fact]
	public void CreateCustomEventEntry_WithIsLockedTrueOrNull_CreatesLockedEntry()
	{
		var lockedEntry = TimelineEntry.CreateCustomEventEntry(
			TripId, DestinationId, DayNumber, OrderIndex,
			"Reserved Event", new TimeOnly(14, 0), 60,
			isLocked: true);
		var defaultEntry = TimelineEntry.CreateCustomEventEntry(
			TripId, DestinationId, DayNumber, OrderIndex,
			"Default Event", new TimeOnly(15, 0), 60);

		lockedEntry.IsLocked.Should().BeTrue();
		defaultEntry.IsLocked.Should().BeTrue();
	}

	[Fact]
	public void CreateCustomEventEntry_EmptyName_Throws()
	{
		var act = () => TimelineEntry.CreateCustomEventEntry(
			TripId, DestinationId, DayNumber, OrderIndex,
			"", new TimeOnly(19, 30), 180);

		act.Should().Throw<DomainException>()
			.WithMessage("*CustomName is required for a CustomEvent entry*");
	}

	[Fact]
	public void CreateCustomEventEntry_ZeroDuration_Throws()
	{
		var act = () => TimelineEntry.CreateCustomEventEntry(
			TripId, DestinationId, DayNumber, OrderIndex,
			"Coldplay Konseri", new TimeOnly(19, 30), 0);

		act.Should().Throw<DomainException>()
			.WithMessage("*DurationMinutes must be greater than 0*");
	}

	[Fact]
	public void CreateCustomEventEntry_NegativeDuration_Throws()
	{
		var act = () => TimelineEntry.CreateCustomEventEntry(
			TripId, DestinationId, DayNumber, OrderIndex,
			"Coldplay Konseri", new TimeOnly(19, 30), -10);

		act.Should().Throw<DomainException>()
			.WithMessage("*DurationMinutes must be greater than 0*");
	}

	// ------------------------------------------------------------------
	// Domain methods
	// ------------------------------------------------------------------
	[Fact]
	public void MarkVisited_SetsIsVisitedAndVisitedAt()
	{
		var entry = TimelineEntry.CreatePlaceEntry(TripId, DestinationId, DayNumber, OrderIndex, PlaceId);
		var before = DateTime.UtcNow.AddSeconds(-1);

		entry.MarkVisited();

		entry.IsVisited.Should().BeTrue();
		entry.VisitedAt.Should().NotBeNull();
		entry.VisitedAt.Should().BeAfter(before);
	}

	[Fact]
	public void MarkUnvisited_ClearsVisitedAt()
	{
		var entry = TimelineEntry.CreatePlaceEntry(TripId, DestinationId, DayNumber, OrderIndex, PlaceId);
		entry.MarkVisited();

		entry.MarkUnvisited();

		entry.IsVisited.Should().BeFalse();
		entry.VisitedAt.Should().BeNull();
	}

	// ------------------------------------------------------------------
	// Property defaults & access modifiers
	// ------------------------------------------------------------------
	[Fact]
	public void OrderIndex_HasPublicSetter()
	{
		var property = typeof(TimelineEntry).GetProperty(nameof(TimelineEntry.OrderIndex));
		property.Should().NotBeNull();
		property!.SetMethod.Should().NotBeNull();
		property.SetMethod!.IsPublic.Should().BeTrue();
	}

	[Fact]
	public void Price_DefaultsToZero()
	{
		var entry = TimelineEntry.CreatePlaceEntry(TripId, DestinationId, DayNumber, OrderIndex, PlaceId);
		entry.Price.Should().Be(0m);
	}

	[Fact]
	public void CurrencyCode_DefaultsToUsd()
	{
		var entry = TimelineEntry.CreatePlaceEntry(TripId, DestinationId, DayNumber, OrderIndex, PlaceId);
		entry.CurrencyCode.Should().Be("USD");
	}

	[Fact]
	public void EntryType_CannotBeChangedAfterCreation()
	{
		var property = typeof(TimelineEntry).GetProperty(nameof(TimelineEntry.EntryType));
		property.Should().NotBeNull();
		property!.SetMethod.Should().NotBeNull();
		property.SetMethod!.IsPublic.Should().BeFalse();
	}

	[Fact]
	public void IsLocked_CannotBeChangedExternally()
	{
		var property = typeof(TimelineEntry).GetProperty(nameof(TimelineEntry.IsLocked));
		property.Should().NotBeNull();
		property!.SetMethod.Should().NotBeNull();
		property.SetMethod!.IsPublic.Should().BeFalse();
	}

	[Fact]
	public void BufferMinutes_CannotBeChangedExternally()
	{
		var property = typeof(TimelineEntry).GetProperty(nameof(TimelineEntry.BufferMinutes));
		property.Should().NotBeNull();
		property!.SetMethod.Should().NotBeNull();
		property.SetMethod!.IsPublic.Should().BeFalse();
	}
}
