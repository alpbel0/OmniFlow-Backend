using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;
using OmniFlow.Domain.Exceptions;

namespace OmniFlow.UnitTests.Phase1;

public class TripDestinationEntityTests
{
	[Fact]
	public void Constructor_ValidInput_SetsNightCountCorrectly()
	{
		var arrival = new DateOnly(2026, 8, 10);
		var departure = new DateOnly(2026, 8, 13);

		var dest = new TripDestination(arrival, departure, "Paris", "France", 1);

		dest.NightCount.Should().Be(3);
		dest.ArrivalDate.Should().Be(arrival);
		dest.DepartureDate.Should().Be(departure);
		dest.City.Should().Be("Paris");
		dest.Country.Should().Be("France");
		dest.OrderIndex.Should().Be(1);
	}

	[Fact]
	public void Constructor_DayTrip_SetsZeroNightCount()
	{
		var date = new DateOnly(2026, 8, 10);

		var dest = new TripDestination(date, date, "Brussels", "Belgium", 2);

		dest.NightCount.Should().Be(0);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(4)]
	[InlineData(-1)]
	public void Constructor_InvalidOrderIndex_Throws(int orderIndex)
	{
		var act = () => new TripDestination(
			new DateOnly(2026, 8, 10),
			new DateOnly(2026, 8, 13),
			"Paris",
			"France",
			orderIndex);

		act.Should().Throw<DomainException>()
			.WithMessage("*OrderIndex must be between 1 and 3*");
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void Constructor_EmptyCity_Throws(string? city)
	{
		var act = () => new TripDestination(
			new DateOnly(2026, 8, 10),
			new DateOnly(2026, 8, 13),
			city!,
			"France",
			1);

		act.Should().Throw<DomainException>()
			.WithMessage("*City is required*");
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void Constructor_EmptyCountry_Throws(string? country)
	{
		var act = () => new TripDestination(
			new DateOnly(2026, 8, 10),
			new DateOnly(2026, 8, 13),
			"Paris",
			country!,
			1);

		act.Should().Throw<DomainException>()
			.WithMessage("*Country is required*");
	}

	[Fact]
	public void Constructor_DepartureBeforeArrival_Throws()
	{
		var act = () => new TripDestination(
			new DateOnly(2026, 8, 13),
			new DateOnly(2026, 8, 10),
			"Paris",
			"France",
			1);

		act.Should().Throw<DomainException>()
			.WithMessage("*DepartureDate cannot be earlier than ArrivalDate*");
	}

	[Fact]
	public void UpdateDates_SameDay_RecalculatesToZero()
	{
		var dest = new TripDestination(
			new DateOnly(2026, 8, 10),
			new DateOnly(2026, 8, 13),
			"Paris",
			"France",
			1);

		dest.UpdateDates(new DateOnly(2026, 8, 15), new DateOnly(2026, 8, 15));

		dest.NightCount.Should().Be(0);
		dest.ArrivalDate.Should().Be(new DateOnly(2026, 8, 15));
		dest.DepartureDate.Should().Be(new DateOnly(2026, 8, 15));
	}

	[Fact]
	public void UpdateDates_DepartureBeforeArrival_Throws()
	{
		var dest = new TripDestination(
			new DateOnly(2026, 8, 10),
			new DateOnly(2026, 8, 13),
			"Paris",
			"France",
			1);

		var act = () => dest.UpdateDates(new DateOnly(2026, 8, 15), new DateOnly(2026, 8, 14));

		act.Should().Throw<DomainException>()
			.WithMessage("*DepartureDate cannot be earlier than ArrivalDate*");
	}

	[Fact]
	public void UpdateCity_TrimsWhitespace()
	{
		var dest = new TripDestination(
			new DateOnly(2026, 8, 10),
			new DateOnly(2026, 8, 13),
			"Paris",
			"France",
			1);

		dest.UpdateCity("  Brussels  ", "  Belgium  ");

		dest.City.Should().Be("Brussels");
		dest.Country.Should().Be("Belgium");
	}

	[Fact]
	public void NightCount_CannotBeSetExternally()
	{
		var property = typeof(TripDestination).GetProperty(nameof(TripDestination.NightCount));
		property.Should().NotBeNull();
		property!.SetMethod.Should().NotBeNull();
		property.SetMethod!.IsPublic.Should().BeFalse();
	}

	[Fact]
	public void ArrivalDate_Setter_IsPrivate()
	{
		var property = typeof(TripDestination).GetProperty(nameof(TripDestination.ArrivalDate));
		property.Should().NotBeNull();
		property!.SetMethod.Should().NotBeNull();
		property.SetMethod!.IsPublic.Should().BeFalse();
	}

	[Fact]
	public void DepartureDate_Setter_IsPrivate()
	{
		var property = typeof(TripDestination).GetProperty(nameof(TripDestination.DepartureDate));
		property.Should().NotBeNull();
		property!.SetMethod.Should().NotBeNull();
		property.SetMethod!.IsPublic.Should().BeFalse();
	}
}
