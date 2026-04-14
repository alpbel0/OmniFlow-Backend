using MediatR;
using OmniFlow.Application.DTOs.Trips;

namespace OmniFlow.Application.Features.Trips.Queries.GetFeaturedTrips;

public class GetFeaturedTripsQuery : IRequest<IReadOnlyList<FeaturedTripResponse>>
{
	public int Limit { get; set; } = 6;
}
