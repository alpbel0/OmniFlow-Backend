using OmniFlow.Domain.Entities;

namespace OmniFlow.Application.Interfaces;

public interface ITripVisibilityService
{
	bool CanRead(Trip trip, Guid? currentUserId);
	void EnsureVisibleOrThrow(Trip trip, string? currentUserIdString);
}
