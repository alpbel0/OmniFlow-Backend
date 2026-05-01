namespace OmniFlow.Application.DTOs.Trips;

public class TimelineSummary
{
    public int TotalEntryCount { get; set; }
    public List<DailyEntryCount> DailyCounts { get; set; } = new();
}
