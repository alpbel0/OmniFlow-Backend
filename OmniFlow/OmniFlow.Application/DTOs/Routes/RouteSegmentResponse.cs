namespace OmniFlow.Application.DTOs.Routes;

public class RouteSegmentResponse
{
    public Guid FromDestinationId { get; set; }

    public Guid ToDestinationId { get; set; }

    public RouteDetailDto? Walking { get; set; }

    public RouteDetailDto? Cycling { get; set; }

    public RouteDetailDto? Driving { get; set; }
}
