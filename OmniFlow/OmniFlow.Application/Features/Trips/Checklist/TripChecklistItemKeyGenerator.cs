using OmniFlow.Domain.Entities;

namespace OmniFlow.Application.Features.Trips.Checklist;

public static class TripChecklistItemKeyGenerator
{
	public static IReadOnlyList<string> Generate(IEnumerable<TripDestination> destinations)
	{
		var orderedDestinations = destinations
			.OrderBy(d => d.OrderIndex)
			.ThenBy(d => d.Id)
			.ToList();

		var itemKeys = new List<string>();

		for (var index = 0; index < orderedDestinations.Count - 1; index++)
		{
			itemKeys.Add(CreateFlightLegKey(orderedDestinations[index].Id, orderedDestinations[index + 1].Id));
		}

		foreach (var destination in orderedDestinations)
		{
			for (var nightNumber = 1; nightNumber <= destination.NightCount; nightNumber++)
			{
				itemKeys.Add(CreateHotelNightKey(destination.Id, nightNumber));
			}
		}

		return itemKeys;
	}

	public static bool BelongsToDestination(string itemKey, Guid destinationId)
	{
		var destinationToken = destinationId.ToString("D");

		if (itemKey.StartsWith("hotel-night:", StringComparison.OrdinalIgnoreCase))
		{
			var parts = itemKey.Split(':');
			return parts.Length == 3 && parts[1].Equals(destinationToken, StringComparison.OrdinalIgnoreCase);
		}

		if (itemKey.StartsWith("flight-leg:", StringComparison.OrdinalIgnoreCase))
		{
			var parts = itemKey.Split(':');
			return parts.Length == 3 &&
				(parts[1].Equals(destinationToken, StringComparison.OrdinalIgnoreCase) ||
				 parts[2].Equals(destinationToken, StringComparison.OrdinalIgnoreCase));
		}

		return false;
	}

	private static string CreateFlightLegKey(Guid fromDestinationId, Guid toDestinationId)
	{
		return $"flight-leg:{fromDestinationId:D}:{toDestinationId:D}";
	}

	private static string CreateHotelNightKey(Guid destinationId, int nightNumber)
	{
		return $"hotel-night:{destinationId:D}:{nightNumber}";
	}
}
