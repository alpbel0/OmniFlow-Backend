using OmniFlow.Domain.Common;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Domain.Entities;

public class Trip : AuditableBaseEntity
{
	public Guid OwnerId { get; set; }

	public Guid? ForkedFromId { get; set; }

	public string Title { get; set; } = string.Empty;

	public string? Description { get; set; }

	public string? CoverPhotoUrl { get; set; }

	public TripStatus Status { get; set; } = TripStatus.Draft;

	// Origin city (departure point - Step 1 of wizard)
	public string Origin { get; set; } = string.Empty;

	public string OriginCountry { get; set; } = string.Empty;

	public double? OriginLatitude { get; private set; }

	public double? OriginLongitude { get; private set; }

	// Date range computed from Destinations
	public DateOnly StartDate { get; private set; }

	public DateOnly EndDate { get; private set; }

	public int PersonCount { get; set; } = 1;

	public BudgetTier BudgetTier { get; set; }

	// Step 4: Travel Companion
	public TravelCompanion TravelCompanion { get; set; }

	// Step 6: Vibe (max 3 travel styles)
	public List<TravelStyle> TravelStyles { get; set; } = new();

	// Step 7: Tempo
	public Tempo Tempo { get; set; }

	// Step 8: Transport Preference
	public TransportPreference TransportPreference { get; set; }

	// Step 5: Manual budget input
	public decimal? ManualBudget { get; set; }

	// Step 5: Budget tier after fallback calculation
	public BudgetTier? AdjustedBudgetTier { get; set; }

	public decimal? EstimatedCost { get; set; }

	public int ForkCount { get; set; } = 0;

	public int UpvoteCount { get; set; } = 0;

	public int ViewCount { get; set; } = 0;

	public decimal PopularityScore { get; set; } = 0;

	public List<string> Tags { get; set; } = new();

	// Navigation properties
	public User? Owner { get; set; }

	public ICollection<TripDestination> Destinations { get; set; } = new List<TripDestination>();

	public ICollection<TimelineEntry> TimelineEntries { get; set; } = new List<TimelineEntry>();

	public ICollection<Flight> Flights { get; set; } = new List<Flight>();

	public ICollection<Hotel> Hotels { get; set; } = new List<Hotel>();

	public void SetOriginCoordinates(double? latitude, double? longitude)
	{
		OriginLatitude = latitude;
		OriginLongitude = longitude;
	}

	/// <summary>
	/// Recalculates StartDate and EndDate from the Destinations collection.
	/// IMPORTANT: Destinations must be eager-loaded (.Include) before calling this method.
	/// Never call this from a collection setter or property accessor to avoid EF Core side-effects.
	/// </summary>
	/// <remarks>
	/// This method does NOT depend on OrderIndex. It only uses ArrivalDate/DepartureDate Min/Max values.
	/// Safe to call after ExecuteUpdateAsync OrderIndex shifts because date values remain unaffected.
	/// </remarks>
	public void RecalculateFromDestinations()
	{
		if (Destinations == null)
			throw new InvalidOperationException(
				"Destinations must be loaded before calling RecalculateFromDestinations.");

		if (Destinations.Any())
		{
			StartDate = Destinations.Min(d => d.ArrivalDate);
			EndDate = Destinations.Max(d => d.DepartureDate);
		}
		else
		{
			StartDate = default;
			EndDate = default;
		}
	}
}
