using MediatR;

namespace OmniFlow.Application.Features.Trips.Queries.ExploreTrips;

public class ExploreTripsQuery : IRequest<ExploreTripsViewModel>
{
    public ExploreTripsParameter Parameter { get; set; }

    public ExploreTripsQuery(ExploreTripsParameter parameter)
    {
        Parameter = parameter;
    }
}