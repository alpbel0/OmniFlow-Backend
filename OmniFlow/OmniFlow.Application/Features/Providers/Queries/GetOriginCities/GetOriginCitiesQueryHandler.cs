using MediatR;
using OmniFlow.Application.DTOs.Providers;
using OmniFlow.Application.Interfaces.Repositories;

namespace OmniFlow.Application.Features.Providers.Queries.GetOriginCities;

public class GetOriginCitiesQueryHandler : IRequestHandler<GetOriginCitiesQuery, IReadOnlyList<OriginCityResponse>>
{
    private readonly IProviderFlightRepositoryAsync _flightRepository;

    public GetOriginCitiesQueryHandler(IProviderFlightRepositoryAsync flightRepository)
    {
        _flightRepository = flightRepository;
    }

    public async Task<IReadOnlyList<OriginCityResponse>> Handle(GetOriginCitiesQuery request, CancellationToken cancellationToken)
    {
        var cities = await _flightRepository.GetDistinctDepartureCitiesAsync();

        return cities
            .Select(f => new OriginCityResponse
            {
                City = f.DepartureCity,
                Country = string.Empty,
                AirportCode = f.DepartureAirportCode
            })
            .GroupBy(c => c.City, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(c => c.City)
            .ToList();
    }
}