namespace OmniFlow.Application.DTOs.TripDestinations;

public class CreateTripDestinationRequest
{
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public DateOnly ArrivalDate { get; set; }
    public DateOnly DepartureDate { get; set; }
    public int OrderIndex { get; set; }
}
