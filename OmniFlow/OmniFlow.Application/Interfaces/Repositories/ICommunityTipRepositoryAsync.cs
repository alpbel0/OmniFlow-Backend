using OmniFlow.Application.Parameters;
using OmniFlow.Application.Wrappers;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Application.Interfaces.Repositories;

public interface ICommunityTipRepositoryAsync : IGenericRepositoryAsync<CommunityTip>
{
	Task<PagedResponse<CommunityTip>> GetByTripAsync(Guid tripId, RequestParameter parameter);
	Task<IReadOnlyList<CommunityTip>> GetByPlaceInTripAsync(Guid tripId, Guid placeId);
}
