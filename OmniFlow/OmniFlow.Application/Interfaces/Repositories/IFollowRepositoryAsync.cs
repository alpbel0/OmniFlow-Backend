using OmniFlow.Application.Parameters;
using OmniFlow.Application.Wrappers;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Application.Interfaces.Repositories;

public interface IFollowRepositoryAsync
{
	Task<PagedResponse<Follow>> GetFollowersAsync(Guid userId, RequestParameter parameter);
	Task<PagedResponse<Follow>> GetFollowingAsync(Guid userId, RequestParameter parameter);
	Task<bool> IsFollowingAsync(Guid followerId, Guid followingId);
}
