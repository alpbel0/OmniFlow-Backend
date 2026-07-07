namespace OmniFlow.Application.DTOs.Routes;

public class TripRoutesResponse
{
    public Guid TripId { get; set; }

    public List<RouteSegmentResponse> Segments { get; set; } = new();
}
