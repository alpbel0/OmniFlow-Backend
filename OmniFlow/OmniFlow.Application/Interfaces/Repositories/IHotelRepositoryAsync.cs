using OmniFlow.Domain.Entities;

namespace OmniFlow.Application.Interfaces.Repositories;

public interface IHotelRepositoryAsync : IGenericRepositoryAsync<Hotel>
{
    /// <summary>
    /// Gets all hotels for a trip, ordered by check-in date ascending.
    /// </summary>
    /// <param name="tripId">The trip ID to filter by.</param>
    /// <returns>List of hotels for the trip, sorted by CheckIn date.</returns>
    Task<IReadOnlyList<Hotel>> GetByTripAsync(Guid tripId);

    /// <summary>
    /// Gets all booked hotels for a trip.
    /// Used for cancelling previous bookings when selecting a new hotel.
    /// </summary>
    /// <param name="tripId">The trip ID.</param>
    /// <returns>List of booked hotels for the trip.</returns>
    Task<IReadOnlyList<Hotel>> GetBookedHotelsByTripAsync(Guid tripId);
}