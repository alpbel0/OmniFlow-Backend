using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Repositories;

public class ProviderFlightRepositoryAsync : GenericRepositoryAsync<ProviderFlight>, IProviderFlightRepositoryAsync
{
    public ProviderFlightRepositoryAsync(IApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<ProviderFlight>> GetByRouteAsync(string fromCity, string toCity, DateOnly date)
    {
        var dateTime = date.ToDateTime(TimeOnly.MinValue);

        return await _dbSet
            .Where(f => f.DepartureCity == fromCity
                     && f.ArrivalCity == toCity
                     && f.DepartureTime.Date == dateTime.Date)
            .OrderBy(f => f.Price)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<ProviderFlight>> GetByRouteAsync(string fromCity, string toCity)
    {
        return await _dbSet
            .Where(f => f.DepartureCity == fromCity
                     && f.ArrivalCity == toCity)
            .OrderBy(f => f.DepartureTime)
            .ThenBy(f => f.Price)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<ProviderFlight>> GetDistinctDepartureCitiesAsync()
    {
        var flights = await _dbSet.ToListAsync();
        return flights
            .GroupBy(f => new { f.DepartureCity, f.DepartureAirportCode })
            .Select(g => g.First())
            .OrderBy(f => f.DepartureCity)
            .ToList();
    }
}
