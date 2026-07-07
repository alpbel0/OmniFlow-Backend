namespace OmniFlow.Application.DTOs.Routes;

public class RouteDetailDto
{
    public List<List<double>> Coordinates { get; set; } = new();

    public double DistanceMeters { get; set; }

    public double DurationSeconds { get; set; }

    public static RouteDetailDto Empty() => new();
}
