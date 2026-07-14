namespace OmniFlow.Application.DTOs.Trips;

public class TripDestinationResponse
{
    public Guid Id { get; set; }
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Timezone { get; set; }
    public DateOnly ArrivalDate { get; set; }
    public DateOnly DepartureDate { get; set; }
    public int OrderIndex { get; set; }
    public int NightCount { get; set; }
}
