namespace OmniFlow.Application.DTOs.Trips;

public class FeaturedTripResponse
{
	public Guid Id { get; set; }
	public string Title { get; set; } = string.Empty;
	public string? CoverPhotoUrl { get; set; }
	public string Origin { get; set; } = string.Empty;
	public string OriginCountry { get; set; } = string.Empty;
	public int ForkCount { get; set; }
	public int UpvoteCount { get; set; }
	public decimal PopularityScore { get; set; }
	public DateOnly StartDate { get; set; }
	public DateOnly EndDate { get; set; }
	public Guid OwnerId { get; set; }
	public string OwnerUsername { get; set; } = string.Empty;
	public string? OwnerProfilePhotoUrl { get; set; }
}
