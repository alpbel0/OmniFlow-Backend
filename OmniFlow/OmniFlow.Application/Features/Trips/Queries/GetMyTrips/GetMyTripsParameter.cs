using OmniFlow.Application.Parameters;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.Trips.Queries.GetMyTrips;

public class GetMyTripsParameter : RequestParameter
{
    public TripStatus? Status { get; set; }
}