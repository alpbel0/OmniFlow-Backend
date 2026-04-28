using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.DTOs.Trips;

public class ScoredPlaceResult
{
    public Place Place { get; set; } = null!;
    public int FinalScore { get; set; }
    public int GroupScore { get; set; }
    public int StyleScoreAvg { get; set; }
    public int GoogleMatchBonus { get; set; }
}
