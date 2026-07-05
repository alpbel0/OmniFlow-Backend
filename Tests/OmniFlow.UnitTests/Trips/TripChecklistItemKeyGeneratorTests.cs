using OmniFlow.Application.Features.Trips.Checklist;
using OmniFlow.Domain.Entities;

namespace OmniFlow.UnitTests.Trips;

public class TripChecklistItemKeyGeneratorTests
{
	[Fact]
	public void Generate_WithMultipleDestinations_ReturnsConsecutiveFlightLegKeys()
	{
		var firstDestination = CreateDestination(1, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 2));
		var secondDestination = CreateDestination(2, new DateOnly(2026, 1, 2), new DateOnly(2026, 1, 3));
		var thirdDestination = CreateDestination(3, new DateOnly(2026, 1, 3), new DateOnly(2026, 1, 4));

		var itemKeys = TripChecklistItemKeyGenerator.Generate([thirdDestination, firstDestination, secondDestination]);

		itemKeys.Should().ContainInOrder(
			$"flight-leg:{firstDestination.Id:D}:{secondDestination.Id:D}",
			$"flight-leg:{secondDestination.Id:D}:{thirdDestination.Id:D}");
	}

	[Fact]
	public void Generate_WithNightCount_ReturnsHotelNightKeys()
	{
		var destination = CreateDestination(1, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 4));

		var itemKeys = TripChecklistItemKeyGenerator.Generate([destination]);

		itemKeys.Should().BeEquivalentTo([
			$"hotel-night:{destination.Id:D}:1",
			$"hotel-night:{destination.Id:D}:2",
			$"hotel-night:{destination.Id:D}:3"
		], options => options.WithStrictOrdering());
	}

	[Fact]
	public void Generate_WithZeroNightDestination_DoesNotReturnHotelNightKeys()
	{
		var destination = CreateDestination(1, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 1));

		var itemKeys = TripChecklistItemKeyGenerator.Generate([destination]);

		itemKeys.Should().BeEmpty();
	}

	[Fact]
	public void BelongsToDestination_WithRelatedFlightAndHotelKeys_ReturnsTrue()
	{
		var destinationId = Guid.NewGuid();
		var otherDestinationId = Guid.NewGuid();

		TripChecklistItemKeyGenerator
			.BelongsToDestination($"flight-leg:{destinationId:D}:{otherDestinationId:D}", destinationId)
			.Should().BeTrue();
		TripChecklistItemKeyGenerator
			.BelongsToDestination($"hotel-night:{destinationId:D}:1", destinationId)
			.Should().BeTrue();
	}

	private static TripDestination CreateDestination(int orderIndex, DateOnly arrivalDate, DateOnly departureDate)
	{
		return new TripDestination(arrivalDate, departureDate, $"City {orderIndex}", "Country", orderIndex);
	}
}
