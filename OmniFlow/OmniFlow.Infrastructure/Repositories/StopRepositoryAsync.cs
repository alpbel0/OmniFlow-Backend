using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Repositories;

public class StopRepositoryAsync : GenericRepositoryAsync<Stop>, IStopRepositoryAsync
{
    public StopRepositoryAsync(IApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Stop>> GetByTripAsync(Guid tripId)
    {
        return await _dbSet
            .Include(s => s.Place)
            .Include(s => s.FallbackPlace)
            .Where(s => s.TripId == tripId && s.DeletedAt == null)
            .OrderBy(s => s.DayNumber)
            .ThenBy(s => s.OrderIndex)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Stop>> GetByTripAndDayAsync(Guid tripId, int dayNumber)
    {
        return await _dbSet
            .Include(s => s.Place)
            .Include(s => s.FallbackPlace)
            .Where(s => s.TripId == tripId && s.DayNumber == dayNumber && s.DeletedAt == null)
            .OrderBy(s => s.OrderIndex)
            .ToListAsync();
    }

    public async Task<Stop?> GetByIdWithPlaceAsync(Guid stopId)
    {
        return await _dbSet
            .Include(s => s.Place)
            .Include(s => s.FallbackPlace)
            .FirstOrDefaultAsync(s => s.Id == stopId && s.DeletedAt == null);
    }

    public async Task<Stop?> GetLastStopInDayAsync(Guid tripId, int dayNumber)
    {
        return await _dbSet
            .Where(s => s.TripId == tripId && s.DayNumber == dayNumber && s.DeletedAt == null)
            .OrderByDescending(s => s.OrderIndex)
            .FirstOrDefaultAsync();
    }
}