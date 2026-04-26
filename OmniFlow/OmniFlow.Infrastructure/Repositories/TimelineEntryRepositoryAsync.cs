using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Repositories;

public class TimelineEntryRepositoryAsync : GenericRepositoryAsync<TimelineEntry>, ITimelineEntryRepositoryAsync
{
	public TimelineEntryRepositoryAsync(IApplicationDbContext context) : base(context)
	{
	}

	public async Task<IReadOnlyList<TimelineEntry>> GetByTripAsync(Guid tripId)
	{
		return await _dbSet
			.AsNoTracking()
			.Include(e => e.Place)
			.Include(e => e.ProviderFlight)
			.Include(e => e.ProviderHotel)
			.Include(e => e.Destination)
			.Where(e => e.TripId == tripId && e.DeletedAt == null)
			.OrderBy(e => e.Destination!.OrderIndex)
			.ThenBy(e => e.DayNumber)
			.ThenBy(e => e.OrderIndex)
			.ToListAsync();
	}

	public async Task<IReadOnlyList<TimelineEntry>> GetByDestinationAsync(Guid destinationId)
	{
		return await _dbSet
			.AsNoTracking()
			.Include(e => e.Place)
			.Include(e => e.ProviderFlight)
			.Include(e => e.ProviderHotel)
			.Where(e => e.DestinationId == destinationId && e.DeletedAt == null)
			.OrderBy(e => e.DayNumber)
			.ThenBy(e => e.OrderIndex)
			.ToListAsync();
	}

	public async Task<IReadOnlyList<TimelineEntry>> GetByTripAndDayAsync(Guid tripId, Guid destinationId, int dayNumber)
	{
		return await _dbSet
			.AsNoTracking()
			.Include(e => e.Place)
			.Include(e => e.ProviderFlight)
			.Include(e => e.ProviderHotel)
			.Where(e => e.TripId == tripId && e.DestinationId == destinationId && e.DayNumber == dayNumber && e.DeletedAt == null)
			.OrderBy(e => e.OrderIndex)
			.ToListAsync();
	}

	public async Task<TimelineEntry?> GetByIdWithPlaceAsync(Guid entryId)
	{
		return await _dbSet
			.AsNoTracking()
			.Include(e => e.Place)
			.Include(e => e.ProviderFlight)
			.Include(e => e.ProviderHotel)
			.Include(e => e.Destination)
			.FirstOrDefaultAsync(e => e.Id == entryId && e.DeletedAt == null);
	}

	public async Task<TimelineEntry?> GetLastEntryInDayAsync(Guid tripId, Guid destinationId, int dayNumber)
	{
		return await _dbSet
			.Where(e => e.TripId == tripId && e.DestinationId == destinationId && e.DayNumber == dayNumber && e.DeletedAt == null)
			.OrderByDescending(e => e.OrderIndex)
			.FirstOrDefaultAsync();
	}
}
