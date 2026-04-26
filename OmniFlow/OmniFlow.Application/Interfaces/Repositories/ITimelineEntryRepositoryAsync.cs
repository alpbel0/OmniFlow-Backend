using OmniFlow.Domain.Entities;

namespace OmniFlow.Application.Interfaces.Repositories;

public interface ITimelineEntryRepositoryAsync : IGenericRepositoryAsync<TimelineEntry>
{
	/// <summary>
	/// Gets all timeline entries for a trip, sorted by destination, day, then order index.
	/// Includes Place, ProviderFlight, ProviderHotel, and Destination navigations.
	/// </summary>
	Task<IReadOnlyList<TimelineEntry>> GetByTripAsync(Guid tripId);

	/// <summary>
	/// Gets all timeline entries for a specific destination.
	/// Includes Place, ProviderFlight, and ProviderHotel navigations.
	/// </summary>
	Task<IReadOnlyList<TimelineEntry>> GetByDestinationAsync(Guid destinationId);

	/// <summary>
	/// Gets all timeline entries for a specific day within a destination.
	/// Includes Place, ProviderFlight, and ProviderHotel navigations.
	/// </summary>
	Task<IReadOnlyList<TimelineEntry>> GetByTripAndDayAsync(Guid tripId, Guid destinationId, int dayNumber);

	/// <summary>
	/// Gets a timeline entry by ID with Place included.
	/// </summary>
	Task<TimelineEntry?> GetByIdWithPlaceAsync(Guid entryId);

	/// <summary>
	/// Gets the last entry in a specific day for OrderIndex auto-calculation (LexoRank).
	/// </summary>
	Task<TimelineEntry?> GetLastEntryInDayAsync(Guid tripId, Guid destinationId, int dayNumber);
}
