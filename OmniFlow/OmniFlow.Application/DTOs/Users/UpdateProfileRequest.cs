using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.DTOs.Users;

public class UpdateProfileRequest
{
	public string? Bio { get; set; }
	public string? ProfilePhotoUrl { get; set; }
	public string? Location { get; set; }
	public double? LocationLatitude { get; set; }
	public double? LocationLongitude { get; set; }
	public List<TravelStyle>? TravelStyles { get; set; }
}
