using OmniFlow.Domain.Entities;

namespace OmniFlow.Application.Interfaces.Repositories;

public interface IProviderHotelRepositoryAsync : IGenericRepositoryAsync<ProviderHotel>
{
    Task<IReadOnlyList<decimal>> GetDistinctPricesByCityAsync(string city);
    Task<IReadOnlyList<ProviderHotel>> GetByCityAsync(string city);
    Task<IReadOnlyList<ProviderHotel>> GetByCityAndDateAsync(string city, DateOnly validDate);
}
