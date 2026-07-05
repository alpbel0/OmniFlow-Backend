using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Exceptions;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Services;

public class TripVisibilityService : ITripVisibilityService
{
	public bool CanRead(Trip trip, Guid? currentUserId)
	{
		if (trip.Status == TripStatus.Published)
			return true;

		return currentUserId.HasValue && trip.OwnerId == currentUserId.Value;
	}

	public void EnsureVisibleOrThrow(Trip trip, string? currentUserIdString)
	{
		var currentUserId = Guid.TryParse(currentUserIdString, out var parsedUserId)
			? parsedUserId
			: (Guid?)null;

		if (!CanRead(trip, currentUserId))
			throw new EntityNotFoundException("Trip", trip.Id);
	}
}
