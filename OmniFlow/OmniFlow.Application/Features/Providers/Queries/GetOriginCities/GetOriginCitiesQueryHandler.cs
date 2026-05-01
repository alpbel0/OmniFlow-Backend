using MediatR;
using OmniFlow.Application.DTOs.Providers;
using OmniFlow.Application.Interfaces.Repositories;

namespace OmniFlow.Application.Features.Providers.Queries.GetOriginCities;

public class GetOriginCitiesQueryHandler : IRequestHandler<GetOriginCitiesQuery, IReadOnlyList<OriginCityResponse>>
{
    private static readonly Dictionary<string, string> CityCountryMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Istanbul"] = "Turkey",
        ["Paris"] = "France",
        ["Rome"] = "Italy",
        ["Berlin"] = "Germany",
        ["Antalya"] = "Turkey",
        ["Izmir"] = "Turkey",
        ["Barcelona"] = "Spain",
        ["Amsterdam"] = "Netherlands",
        ["London"] = "United Kingdom",
    };

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
                Country = ResolveCountry(f.DepartureCity),
                AirportCode = f.DepartureAirportCode
            })
            .GroupBy(c => c.City, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(c => c.City)
            .ToList();
    }

    private static string ResolveCountry(string city)
    {
        return CityCountryMap.TryGetValue(city, out var country)
            ? country
            : string.Empty;
    }
}
