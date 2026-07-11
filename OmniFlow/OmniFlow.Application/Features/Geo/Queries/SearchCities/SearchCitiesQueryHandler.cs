using MediatR;
using OmniFlow.Application.DTOs.Geocoding;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Application.Features.Geo.Queries.SearchCities;

public class SearchCitiesQueryHandler : IRequestHandler<SearchCitiesQuery, IReadOnlyList<GeocodingResult>>
{
    private readonly IGeocodingService _geocodingService;

    public SearchCitiesQueryHandler(IGeocodingService geocodingService)
    {
        _geocodingService = geocodingService;
    }

    public async Task<IReadOnlyList<GeocodingResult>> Handle(SearchCitiesQuery request, CancellationToken cancellationToken)
    {
        return await _geocodingService.SearchCitiesAsync(request.Query, request.Limit, cancellationToken);
    }
}
