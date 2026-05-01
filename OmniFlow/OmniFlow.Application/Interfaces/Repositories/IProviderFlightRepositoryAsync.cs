using OmniFlow.Domain.Entities;

namespace OmniFlow.Application.Interfaces.Repositories;

public interface IProviderFlightRepositoryAsync : IGenericRepositoryAsync<ProviderFlight>
{
    Task<IReadOnlyList<ProviderFlight>> GetByRouteAsync(string fromCity, string toCity, DateOnly date);
    Task<IReadOnlyList<ProviderFlight>> GetByRouteAsync(string fromCity, string toCity);
    Task<IReadOnlyList<ProviderFlight>> GetDistinctDepartureCitiesAsync();
}
