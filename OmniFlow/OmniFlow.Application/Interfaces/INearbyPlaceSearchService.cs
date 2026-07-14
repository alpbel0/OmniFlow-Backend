using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Interfaces;

public sealed record NearbyPlaceSearchCriteria(
    Guid TripId,
    double Latitude,
    double Longitude,
    int RadiusMeters,
    IReadOnlyCollection<PlaceCategory> Categories,
    int CandidateLimit);

public sealed record NearbyPlaceCandidate(Guid PlaceId, int DistanceMeters);

public interface INearbyPlaceSearchService
{
    Task<IReadOnlyList<NearbyPlaceCandidate>> SearchAsync(
        NearbyPlaceSearchCriteria criteria,
        CancellationToken cancellationToken = default);
}
