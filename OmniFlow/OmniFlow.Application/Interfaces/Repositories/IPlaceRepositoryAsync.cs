using OmniFlow.Application.Parameters;
using OmniFlow.Application.Wrappers;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Interfaces.Repositories;

public interface IPlaceRepositoryAsync : IGenericRepositoryAsync<Place>
{
    Task<PagedResponse<Place>> GetByCityAsync(string city, RequestParameter parameter);
    Task<PagedResponse<Place>> GetByCategoryAsync(PlaceCategory category, RequestParameter parameter);
}