using OmniFlow.Application.Features.Trips.Queries.SearchNearbyPlaces;

namespace OmniFlow.Application.DTOs.Trips;

public sealed class SearchNearbyPlacesRequest
{
    public Guid TripDestinationId { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public int RadiusKm { get; init; }
    public NearbyPlaceCategoryGroup CategoryGroup { get; init; }
}
