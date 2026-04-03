using OmniFlow.Application.Parameters;
using OmniFlow.Application.Wrappers;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Application.Interfaces.Repositories;

public interface ITripRepositoryAsync : IGenericRepositoryAsync<Trip>
{
    Task<PagedResponse<Trip>> GetByOwnerAsync(Guid ownerId, RequestParameter parameter);
    Task<IReadOnlyList<Trip>> GetPublishedByOwnerAsync(Guid ownerId);
    Task<Trip?> GetWithStopsAsync(Guid tripId);
    Task<Trip?> GetByIdWithOwnerAsync(Guid tripId);
}