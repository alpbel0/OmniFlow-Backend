namespace OmniFlow.Application.DTOs.Trips;

public class RecommendedPlacesResult
{
    public List<ScoredPlaceResponse> Recommended { get; set; } = new();
    public List<ScoredPlaceResponse> Neutral { get; set; } = new();
    public List<ScoredPlaceResponse> Other { get; set; } = new();
    public int DailyCapacity { get; set; }
}
