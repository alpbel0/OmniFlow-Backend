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

	public async Task<PagedResponse<Comment>> GetByPostAsync(Guid postId, RequestParameter parameter)
	{
		var query = _dbSet
			.Include(c => c.User)
			.Include(c => c.Replies)
			.ThenInclude(r => r.User)
			.Where(c => c.PostId == postId && c.ParentCommentId == null)
			.OrderBy(c => c.CreatedAt);

		var totalCount = await query.CountAsync();
		var items = await query
			.Skip((parameter.PageNumber - 1) * parameter.PageSize)
			.Take(parameter.PageSize)
			.ToListAsync();

		return new PagedResponse<Comment>(items, parameter.PageNumber, parameter.PageSize, totalCount);
	}
}
