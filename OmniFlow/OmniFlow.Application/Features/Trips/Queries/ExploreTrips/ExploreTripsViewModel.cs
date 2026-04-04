using OmniFlow.Application.DTOs.Trips;

namespace OmniFlow.Application.Features.Trips.Queries.ExploreTrips;

public class ExploreTripsViewModel
{
    public IReadOnlyList<TripResponse> Data { get; init; } = new List<TripResponse>();
    public string? NextCursor { get; init; }
    public bool HasMore { get; init; }
}