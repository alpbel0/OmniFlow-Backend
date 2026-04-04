using OmniFlow.Domain.Entities;

namespace OmniFlow.Application.Interfaces.Repositories;

public interface IStopRepositoryAsync : IGenericRepositoryAsync<Stop>
{
    /// <summary>
    /// Gets all stops for a trip, sorted by DayNumber then OrderIndex.
    /// Includes Place and FallbackPlace navigation properties.
    /// </summary>
    Task<IReadOnlyList<Stop>> GetByTripAsync(Guid tripId);

    /// <summary>
    /// Gets all stops for a specific day in a trip.
    /// Includes Place and FallbackPlace navigation properties.
    /// </summary>
    Task<IReadOnlyList<Stop>> GetByTripAndDayAsync(Guid tripId, int dayNumber);

    /// <summary>
    /// Gets a stop by ID with Place and FallbackPlace included.
    /// </summary>
    Task<Stop?> GetByIdWithPlaceAsync(Guid stopId);

    /// <summary>
    /// Gets the last stop in a specific day for OrderIndex auto-calculation (LexoRank).
    /// </summary>
    Task<Stop?> GetLastStopInDayAsync(Guid tripId, int dayNumber);
}