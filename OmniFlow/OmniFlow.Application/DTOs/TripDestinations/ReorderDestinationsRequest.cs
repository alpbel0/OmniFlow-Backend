namespace OmniFlow.Application.DTOs.TripDestinations;

public class ReorderDestinationsRequest
{
    public List<Guid> OrderedDestinationIds { get; set; } = new();
}
