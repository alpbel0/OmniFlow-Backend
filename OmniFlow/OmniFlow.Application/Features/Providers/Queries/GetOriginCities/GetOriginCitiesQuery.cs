using MediatR;
using OmniFlow.Application.DTOs.Providers;

namespace OmniFlow.Application.Features.Providers.Queries.GetOriginCities;

public class GetOriginCitiesQuery : IRequest<IReadOnlyList<OriginCityResponse>>
{
}