using MediatR;
using OmniFlow.Application.DTOs.Trips;

namespace OmniFlow.Application.Features.Trips.Queries.GetRecommendedPlaces;

public class GetRecommendedPlacesQuery : IRequest<RecommendedPlacesResult>
{
    public Guid TripId { get; set; }
    public Guid DestinationId { get; set; }
}