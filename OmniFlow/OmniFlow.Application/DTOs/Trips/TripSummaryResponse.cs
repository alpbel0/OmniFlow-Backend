using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.DTOs.Trips;

public sealed class TripSummaryResponse
{
    public Guid TripId { get; set; }
    public TripExecutionState ExecutionState { get; set; }
    public string BaseCurrencyCode { get; set; } = string.Empty;
    public int TotalVisitCount { get; set; }
    public int UniquePlaceCount { get; set; }
    public int VisitedCustomEventCount { get; set; }
    public int SpontaneousVisitCount { get; set; }
    public int VisitedPlannedEntryCount { get; set; }
    public int PlannedVisitableEntryCount { get; set; }
    public decimal? VisitCompletionPercentage { get; set; }
    public decimal PlannedVisitCost { get; set; }
    public decimal ActualVisitCost { get; set; }
    public int VisitsWithCostCount { get; set; }
    public int MissingCostCount { get; set; }
    public int PendingConversionCount { get; set; }
    public int PendingPlannedConversionCount { get; set; }
    public bool IsCostComplete { get; set; }
    public bool IsConversionComplete { get; set; }
    public bool IsPlannedCostComplete => PendingPlannedConversionCount == 0;
    public int UnmappedVisitCount { get; set; }
    public IReadOnlyList<CurrencyAmountBreakdown> OriginalCurrencyBreakdown { get; set; } = [];
    public IReadOnlyList<TripFavoriteResponse> Favorites { get; set; } = [];
    public IReadOnlyList<TripVisitMarkerResponse> VisitMarkers { get; set; } = [];
    public IReadOnlyList<TripSummaryDestinationResponse> Destinations { get; set; } = [];
}

public sealed record CurrencyAmountBreakdown(string CurrencyCode, decimal Amount, int VisitCount);
public sealed record TripFavoriteResponse(Guid Id, string Type, string Name, decimal AveragePersonalRating, int RatedVisitCount, int VisitCount, DateTime LastVisitedAt);
public sealed record TripVisitMarkerResponse(Guid VisitLogId, string Type, double Latitude, double Longitude, DateTime VisitedAt);
public sealed record TripSummaryDestinationResponse(Guid Id, string City, string Country, int OrderIndex, int VisitCount);
