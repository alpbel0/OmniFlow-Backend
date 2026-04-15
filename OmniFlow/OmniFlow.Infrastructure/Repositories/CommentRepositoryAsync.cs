using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Application.Parameters;
using OmniFlow.Application.Wrappers;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Repositories;

public class CommentRepositoryAsync : GenericRepositoryAsync<Comment>, ICommentRepositoryAsync
{
	public CommentRepositoryAsync(IApplicationDbContext context) : base(context)
	{
	}

	public async Task<Comment?> GetByIdWithRepliesAsync(Guid commentId)
	{
		return await _dbSet
			.Include(c => c.User)
			.Include(c => c.Post)
			.Include(c => c.Replies)
			.ThenInclude(r => r.User)
			.FirstOrDefaultAsync(c => c.Id == commentId);
	}

	public async Task<PagedResponse<Comment>> GetByPostAsync(
		Guid postId,
		RequestParameter parameter,
		IReadOnlyCollection<Guid>? blockedUserIds = null,
		CancellationToken cancellationToken = default)
	{
		var blockedIds = blockedUserIds?.ToList() ?? new List<Guid>();

		IQueryable<Comment> query = _dbSet
			.Include(c => c.User)
			.Where(c => c.PostId == postId && c.ParentCommentId == null);

		if (blockedIds.Count > 0)
		{
			query = query
				.Where(c => !blockedIds.Contains(c.UserId))
				.Include(c => c.Replies.Where(r => !blockedIds.Contains(r.UserId)))
				.ThenInclude(r => r.User);
		}
		else
		{
			query = query
				.Include(c => c.Replies)
				.ThenInclude(r => r.User);
		}

		query = query.OrderBy(c => c.CreatedAt);

		var totalCount = await query.CountAsync(cancellationToken);
		var items = await query
			.Skip((parameter.PageNumber - 1) * parameter.PageSize)
			.Take(parameter.PageSize)
			.ToListAsync(cancellationToken);

		return new PagedResponse<Comment>(items, parameter.PageNumber, parameter.PageSize, totalCount);
	}
}
