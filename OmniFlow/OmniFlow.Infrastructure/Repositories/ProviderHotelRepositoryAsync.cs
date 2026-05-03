using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Repositories;

public class ProviderHotelRepositoryAsync : GenericRepositoryAsync<ProviderHotel>, IProviderHotelRepositoryAsync
{
    public ProviderHotelRepositoryAsync(IApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<decimal>> GetDistinctPricesByCityAsync(string city)
    {
        return await _dbSet
            .Where(h => h.City == city)
            .Select(h => h.PricePerNight)
            .Distinct()
            .OrderBy(p => p)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<ProviderHotel>> GetByCityAsync(string city)
    {
        return await _dbSet
            .Where(h => h.City == city)
            .OrderBy(h => h.PricePerNight)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<ProviderHotel>> GetByCityAndDateAsync(string city, DateOnly validDate)
    {
        return await _dbSet
            .Where(h => h.City == city && h.ValidDate == validDate)
            .OrderBy(h => h.PricePerNight)
            .ToListAsync();
    }
}
