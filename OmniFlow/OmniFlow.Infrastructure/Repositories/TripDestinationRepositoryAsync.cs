using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Repositories;

public class TripDestinationRepositoryAsync : GenericRepositoryAsync<TripDestination>, ITripDestinationRepositoryAsync
{
	public TripDestinationRepositoryAsync(IApplicationDbContext context) : base(context)
	{
	}

	public async Task<IReadOnlyList<TripDestination>> GetByTripAsync(Guid tripId)
	{
		return await _dbSet
			.Where(d => d.TripId == tripId && d.DeletedAt == null)
			.OrderBy(d => d.OrderIndex)
			.ToListAsync();
	}
}
