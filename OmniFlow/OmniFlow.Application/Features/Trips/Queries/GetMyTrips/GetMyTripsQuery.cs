using MediatR;
using OmniFlow.Application.Features.Trips.Queries.GetMyTrips;

namespace OmniFlow.Application.Features.Trips.Queries.GetMyTrips;

public class GetMyTripsQuery : IRequest<GetMyTripsViewModel>
{
    public GetMyTripsParameter Parameter { get; set; }

    public GetMyTripsQuery(GetMyTripsParameter parameter)
    {
        Parameter = parameter;
    }
}