using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Application.Parameters;
using OmniFlow.Application.Wrappers;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Infrastructure.Repositories;

public class TripRepositoryAsync : GenericRepositoryAsync<Trip>, ITripRepositoryAsync
{
    public TripRepositoryAsync(IApplicationDbContext context) : base(context)
    {
    }

    public async Task<PagedResponse<Trip>> GetByOwnerAsync(Guid ownerId, RequestParameter parameter)
    {
        var query = _dbSet
            .Include(t => t.Owner)
            .Where(t => t.OwnerId == ownerId && t.DeletedAt == null)
            .OrderByDescending(t => t.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((parameter.PageNumber - 1) * parameter.PageSize)
            .Take(parameter.PageSize)
            .ToListAsync();

        return new PagedResponse<Trip>(items, parameter.PageNumber, parameter.PageSize, totalCount);
    }

    public async Task<PagedResponse<Trip>> GetPublishedByOwnerAsync(Guid ownerId, RequestParameter parameter)
    {
        var query = _dbSet
            .Include(t => t.Owner)
            .Where(t =>
                t.OwnerId == ownerId &&
                t.Status == TripStatus.Published &&
                t.ForkedFromId == null &&
                t.DeletedAt == null)
            .OrderByDescending(t => t.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((parameter.PageNumber - 1) * parameter.PageSize)
            .Take(parameter.PageSize)
            .ToListAsync();

        return new PagedResponse<Trip>(items, parameter.PageNumber, parameter.PageSize, totalCount);
    }

    public async Task<IReadOnlyList<Trip>> GetPublishedByOwnerAsync(Guid ownerId)
    {
        return await _dbSet
            .Include(t => t.Owner)
            .Where(t =>
                t.OwnerId == ownerId &&
                t.Status == TripStatus.Published &&
                t.ForkedFromId == null &&
                t.DeletedAt == null)
            .ToListAsync();
    }

	public async Task<Trip?> GetWithStopsAsync(Guid tripId)
	{
		return await _dbSet
			.Include(t => t.Owner)
			.FirstOrDefaultAsync(t => t.Id == tripId && t.DeletedAt == null);
	}

    public async Task<Trip?> GetByIdWithOwnerAsync(Guid tripId)
    {
        return await _dbSet
            .Include(t => t.Owner)
            .FirstOrDefaultAsync(t => t.Id == tripId && t.DeletedAt == null);
    }

    public async Task<Trip?> GetByIdWithOwnerAndDestinationsAsync(Guid tripId)
    {
        return await _dbSet
            .Include(t => t.Owner)
            .Include(t => t.Destinations.OrderBy(d => d.OrderIndex))
            .FirstOrDefaultAsync(t => t.Id == tripId && t.DeletedAt == null);
    }

	public async Task<Trip?> GetWithAllRelatedDataAsync(Guid tripId)
	{
		return await _dbSet
			.Include(t => t.Flights)
			.Include(t => t.Hotels)
			.Include(t => t.Owner)
			.FirstOrDefaultAsync(t => t.Id == tripId && t.DeletedAt == null);
	}
}