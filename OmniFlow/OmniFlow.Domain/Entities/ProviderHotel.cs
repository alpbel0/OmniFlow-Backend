using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities;

public class ProviderHotel : BaseEntity
{
	public string HotelName { get; set; } = string.Empty;

	public string City { get; set; } = string.Empty;

	public string Country { get; set; } = string.Empty;

	public double? Latitude { get; set; }

	public double? Longitude { get; set; }

	public int? Stars { get; set; }

	public double? Rating { get; set; }

	public int? ReviewCount { get; set; }

	public DateOnly ValidDate { get; set; }

	public decimal PricePerNight { get; set; }

	public string CurrencyCode { get; set; } = string.Empty;

	public string? ThumbnailUrl { get; set; }

	public string ProviderName { get; set; } = string.Empty;

	public string? ProviderUrl { get; set; }

	public bool IsAvailable { get; set; } = true;

	public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

	public bool IsLiveData { get; set; }

	public DateOnly DataSnapshotDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
}
