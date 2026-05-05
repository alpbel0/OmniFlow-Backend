using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Application.Parameters;
using OmniFlow.Application.Wrappers;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Repositories;

public class PostRepositoryAsync : GenericRepositoryAsync<Post>, IPostRepositoryAsync
{
	public PostRepositoryAsync(IApplicationDbContext context) : base(context)
	{
	}

	public async Task<Post?> GetByIdWithUserAsync(Guid postId)
	{
		return await _dbSet
			.Include(p => p.User)
			.FirstOrDefaultAsync(p => p.Id == postId);
	}

	public async Task<PagedResponse<Post>> GetByUserAsync(Guid userId, RequestParameter parameter)
	{
		var query = _dbSet
			.Include(p => p.User)
			.Where(p => p.UserId == userId)
			.OrderByDescending(p => p.CreatedAt);

		var totalCount = await query.CountAsync();
		var items = await query
			.Skip((parameter.PageNumber - 1) * parameter.PageSize)
			.Take(parameter.PageSize)
			.ToListAsync();

		return new PagedResponse<Post>(items, parameter.PageNumber, parameter.PageSize, totalCount);
	}

	public async Task<PagedResponse<Post>> GetVisibleByUserAsync(Guid userId, RequestParameter parameter)
	{
		var query = _dbSet
			.Include(p => p.User)
			.Where(p => p.UserId == userId && p.DeletedAt == null && p.IsVisible)
			.OrderByDescending(p => p.CreatedAt);

		var totalCount = await query.CountAsync();
		var items = await query
			.Skip((parameter.PageNumber - 1) * parameter.PageSize)
			.Take(parameter.PageSize)
			.ToListAsync();

		return new PagedResponse<Post>(items, parameter.PageNumber, parameter.PageSize, totalCount);
	}

	public async Task<PagedResponse<Post>> GetLikedVisibleByUserAsync(
		Guid userId,
		RequestParameter parameter,
		IReadOnlyCollection<Guid>? excludedAuthorIds = null)
	{
		var query = _dbSet
			.Include(p => p.User)
			.Where(post =>
				post.DeletedAt == null &&
				post.IsVisible &&
				_context.PostUpvotes.Any(upvote => upvote.UserId == userId && upvote.PostId == post.Id));

		if (excludedAuthorIds is { Count: > 0 })
		{
			query = query.Where(post => !excludedAuthorIds.Contains(post.UserId));
		}

		query = query
			.OrderByDescending(post => _context.PostUpvotes
				.Where(upvote => upvote.UserId == userId && upvote.PostId == post.Id)
				.Select(upvote => upvote.CreatedAt)
				.FirstOrDefault())
			.ThenByDescending(post => post.Id);

		var totalCount = await query.CountAsync();
		var items = await query
			.Skip((parameter.PageNumber - 1) * parameter.PageSize)
			.Take(parameter.PageSize)
			.ToListAsync();

		return new PagedResponse<Post>(items, parameter.PageNumber, parameter.PageSize, totalCount);
	}

	public async Task<PagedResponse<Post>> GetVisibleAsync(RequestParameter parameter)
	{
		var query = _dbSet
			.Include(p => p.User)
			.Where(p => p.IsVisible)
			.OrderByDescending(p => p.CreatedAt);

		var totalCount = await query.CountAsync();
		var items = await query
			.Skip((parameter.PageNumber - 1) * parameter.PageSize)
			.Take(parameter.PageSize)
			.ToListAsync();

		return new PagedResponse<Post>(items, parameter.PageNumber, parameter.PageSize, totalCount);
	}
}
