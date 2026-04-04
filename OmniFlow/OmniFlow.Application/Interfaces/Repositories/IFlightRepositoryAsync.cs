using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Interfaces.Repositories;

public interface IFlightRepositoryAsync : IGenericRepositoryAsync<Flight>
{
    /// <summary>
    /// Gets all flights for a trip, optionally filtered by direction.
    /// </summary>
    /// <param name="tripId">The trip ID to filter by.</param>
    /// <param name="direction">Optional direction filter (Outbound or Return).</param>
    /// <returns>List of flights for the trip.</returns>
    Task<IReadOnlyList<Flight>> GetByTripAsync(Guid tripId, FlightDirection? direction = null);

    /// <summary>
    /// Gets all flights in an itinerary group (outbound + return pair).
    /// Used for round-trip flight grouping.
    /// </summary>
    /// <param name="itineraryGroupId">The itinerary group ID.</param>
    /// <returns>List of flights in the group.</returns>
    Task<IReadOnlyList<Flight>> GetByGroupAsync(Guid itineraryGroupId);

    /// <summary>
    /// Gets all booked flights for a trip with a specific direction.
    /// Used for cancelling previous bookings when selecting a new flight.
    /// </summary>
    /// <param name="tripId">The trip ID.</param>
    /// <param name="direction">The flight direction to filter by.</param>
    /// <returns>List of booked flights for the trip with the specified direction.</returns>
    Task<IReadOnlyList<Flight>> GetBookedFlightsByDirectionAsync(Guid tripId, FlightDirection direction);
}