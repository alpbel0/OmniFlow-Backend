using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.Wrappers;

namespace OmniFlow.Application.Features.Trips.Queries.GetMyTrips;

public class GetMyTripsViewModel : PagedResponse<TripResponse>
{
    public GetMyTripsViewModel() : base(new List<TripResponse>(), 0, 0, 0)
    {
    }

    public GetMyTripsViewModel(IReadOnlyList<TripResponse> data, int pageNumber, int pageSize, int totalCount)
        : base(data, pageNumber, pageSize, totalCount)
    {
    }
}