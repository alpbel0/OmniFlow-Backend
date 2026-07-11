using MediatR;
using OmniFlow.Application.DTOs.Geocoding;

namespace OmniFlow.Application.Features.Geo.Queries.SearchCities;

public class SearchCitiesQuery : IRequest<IReadOnlyList<GeocodingResult>>
{
    public string Query { get; set; } = string.Empty;
    public int Limit { get; set; } = 8;
}
