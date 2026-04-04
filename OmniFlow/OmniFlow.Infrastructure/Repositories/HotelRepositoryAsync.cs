using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Repositories;

public class HotelRepositoryAsync : GenericRepositoryAsync<Hotel>, IHotelRepositoryAsync
{
    public HotelRepositoryAsync(IApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Hotel>> GetByTripAsync(Guid tripId)
    {
        return await _dbSet
            .Where(h => h.TripId == tripId)
            .OrderBy(h => h.CheckIn)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Hotel>> GetBookedHotelsByTripAsync(Guid tripId)
    {
        return await _dbSet
            .Where(h => h.TripId == tripId && h.IsBooked)
            .ToListAsync();
    }
}