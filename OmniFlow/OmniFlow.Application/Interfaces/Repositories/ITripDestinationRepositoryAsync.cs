using OmniFlow.Domain.Entities;

namespace OmniFlow.Application.Interfaces.Repositories;

public interface ITripDestinationRepositoryAsync : IGenericRepositoryAsync<TripDestination>
{
	Task<IReadOnlyList<TripDestination>> GetByTripAsync(Guid tripId);
}
