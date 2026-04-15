using OmniFlow.Application.Parameters;
using OmniFlow.Application.Wrappers;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Application.Interfaces.Repositories;

public interface ICommentRepositoryAsync : IGenericRepositoryAsync<Comment>
{
	Task<Comment?> GetByIdWithRepliesAsync(Guid commentId);
	Task<PagedResponse<Comment>> GetByPostAsync(Guid postId, RequestParameter parameter, IReadOnlyCollection<Guid>? blockedUserIds = null, CancellationToken cancellationToken = default);
}
