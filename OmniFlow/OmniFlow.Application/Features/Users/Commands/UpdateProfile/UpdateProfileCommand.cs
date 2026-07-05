using MediatR;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.Users.Commands.UpdateProfile;

public class UpdateProfileCommand : IRequest<Unit>
{
	public string? Bio { get; set; }
	public bool UpdateBio { get; set; }
	public string? ProfilePhotoUrl { get; set; }
	public bool UpdateProfilePhotoUrl { get; set; }
	public string? Location { get; set; }
	public bool UpdateLocation { get; set; }
	public double? LocationLatitude { get; set; }
	public double? LocationLongitude { get; set; }
	public bool UpdateLocationCoordinates { get; set; }
	public List<TravelStyle>? TravelStyles { get; set; }
	public bool UpdateTravelStyles { get; set; }
}
