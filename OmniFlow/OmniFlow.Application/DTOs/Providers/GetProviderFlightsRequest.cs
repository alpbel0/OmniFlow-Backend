namespace OmniFlow.Application.DTOs.Providers;

public class GetProviderFlightsRequest
{
    public string? FromCity { get; set; }
    public string? ToCity { get; set; }
    public DateOnly? Date { get; set; }
    public int PersonCount { get; set; } = 1;
    public bool IsReturn { get; set; }
    public Guid? TripId { get; set; }
}