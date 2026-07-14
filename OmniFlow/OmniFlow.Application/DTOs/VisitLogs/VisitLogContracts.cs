using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.DTOs.VisitLogs;

public sealed class CreateVisitLogRequest
{
    public Guid? TimelineEntryId { get; set; }
    public Guid? PlaceId { get; set; }
    public Guid? TripDestinationId { get; set; }
    public DateTime VisitedAt { get; set; }
    public decimal? ActualCost { get; set; }
    public string? CurrencyCode { get; set; }
    public int? Rating { get; set; }
    public string? Note { get; set; }
}

public sealed class UpdateVisitLogRequest
{
    public Guid? TripDestinationId { get; set; }
    public DateTime VisitedAt { get; set; }
    public decimal? ActualCost { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public int? Rating { get; set; }
    public string? Note { get; set; }
}

public sealed class VisitLogResponse
{
    public Guid Id { get; set; }
    public Guid TripId { get; set; }
    public Guid TripDestinationId { get; set; }
    public Guid? TimelineEntryId { get; set; }
    public Guid? PlaceId { get; set; }
    public string Source => TimelineEntryId.HasValue ? "planned" : "spontaneous";
    public DateTime VisitedAt { get; set; }
    public decimal? ActualCost { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public int? Rating { get; set; }
    public string? Note { get; set; }
    public decimal? ConvertedActualCost { get; set; }
    public decimal? ExchangeRate { get; set; }
    public DateOnly? RateRequestedDate { get; set; }
    public DateOnly? ExchangeRateDate { get; set; }
    public string BaseCurrencyCode { get; set; } = string.Empty;
    public ConversionStatus ConversionStatus { get; set; }
}
