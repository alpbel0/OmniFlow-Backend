using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Application.Parameters;
using OmniFlow.Application.Wrappers;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Repositories;

public class CommunityTipRepositoryAsync : GenericRepositoryAsync<CommunityTip>, ICommunityTipRepositoryAsync
{
	public CommunityTipRepositoryAsync(IApplicationDbContext context) : base(context)
	{
	}

	public async Task<PagedResponse<CommunityTip>> GetByTripAsync(Guid tripId, RequestParameter parameter)
	{
		var query = _dbSet
			.Include(t => t.User)
			.Include(t => t.Place)
			.Where(t => t.TripId == tripId && t.IsVisible)
			.OrderByDescending(t => t.UpvoteCount)
			.ThenByDescending(t => t.CreatedAt);

		var totalCount = await query.CountAsync();
		var items = await query
			.Skip((parameter.PageNumber - 1) * parameter.PageSize)
			.Take(parameter.PageSize)
			.ToListAsync();

		return new PagedResponse<CommunityTip>(items, parameter.PageNumber, parameter.PageSize, totalCount);
	}

	public async Task<IReadOnlyList<CommunityTip>> GetByPlaceInTripAsync(Guid tripId, Guid placeId)
	{
		return await _dbSet
			.Include(t => t.User)
			.Include(t => t.Place)
			.Where(t => t.TripId == tripId && t.PlaceId == placeId && t.IsVisible)
			.OrderByDescending(t => t.UpvoteCount)
			.ThenByDescending(t => t.CreatedAt)
			.ToListAsync();
	}
}
