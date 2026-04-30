using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.DTOs.Providers;

public class ProviderHotelResponse
{
    public Guid Id { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int? Stars { get; set; }
    public double? Rating { get; set; }
    public int? ReviewCount { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string? ProviderUrl { get; set; }

    public decimal BasePricePerNight { get; set; }
    public decimal SeasonAdjustedPricePerNight { get; set; }
    public decimal SeasonMultiplier { get; set; }
    public decimal TotalPrice { get; set; }
    public int NightCount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;

    public BudgetTier Segment { get; set; }
    public bool IsAvailable { get; set; }
}