using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Application.Parameters;
using OmniFlow.Application.Wrappers;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Repositories;

public class FollowRepositoryAsync : IFollowRepositoryAsync
{
	private readonly IApplicationDbContext _context;
	private readonly DbSet<Follow> _dbSet;

	public FollowRepositoryAsync(IApplicationDbContext context)
	{
		_context = context;
		_dbSet = context.Set<Follow>();
	}

	public async Task<PagedResponse<Follow>> GetFollowersAsync(Guid userId, RequestParameter parameter, string? searchTerm = null)
	{
		var query = _dbSet
			.Include(follow => follow.Follower)
			.Where(follow => follow.FollowingId == userId);

		if (!string.IsNullOrWhiteSpace(searchTerm))
		{
			query = query.Where(follow => EF.Functions.ILike(follow.Follower!.Username, $"%{searchTerm}%"));
		}

		query = query.OrderBy(follow => follow.CreatedAt);

		var totalCount = await query.CountAsync();
		var items = await query
			.Skip((parameter.PageNumber - 1) * parameter.PageSize)
			.Take(parameter.PageSize)
			.ToListAsync();

		return new PagedResponse<Follow>(items, parameter.PageNumber, parameter.PageSize, totalCount);
	}

	public async Task<PagedResponse<Follow>> GetFollowingAsync(Guid userId, RequestParameter parameter, string? searchTerm = null)
	{
		var query = _dbSet
			.Include(follow => follow.Following)
			.Where(follow => follow.FollowerId == userId);

		if (!string.IsNullOrWhiteSpace(searchTerm))
		{
			query = query.Where(follow => EF.Functions.ILike(follow.Following!.Username, $"%{searchTerm}%"));
		}

		query = query.OrderBy(follow => follow.CreatedAt);

		var totalCount = await query.CountAsync();
		var items = await query
			.Skip((parameter.PageNumber - 1) * parameter.PageSize)
			.Take(parameter.PageSize)
			.ToListAsync();

		return new PagedResponse<Follow>(items, parameter.PageNumber, parameter.PageSize, totalCount);
	}

	public Task<bool> IsFollowingAsync(Guid followerId, Guid followingId)
	{
		return _dbSet.AnyAsync(follow => follow.FollowerId == followerId && follow.FollowingId == followingId);
	}
}