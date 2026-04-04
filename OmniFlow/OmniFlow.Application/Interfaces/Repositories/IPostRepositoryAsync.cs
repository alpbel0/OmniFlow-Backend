using OmniFlow.Application.Parameters;
using OmniFlow.Application.Wrappers;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Application.Interfaces.Repositories;

public interface IPostRepositoryAsync : IGenericRepositoryAsync<Post>
{
	Task<Post?> GetByIdWithUserAsync(Guid postId);
	Task<PagedResponse<Post>> GetByUserAsync(Guid userId, RequestParameter parameter);
	Task<PagedResponse<Post>> GetVisibleAsync(RequestParameter parameter);
}
