using MediatR;

namespace OmniFlow.Application.Features.Users.Commands.UpdateProfile;

public class UpdateProfileCommand : IRequest<Unit>
{
	public string? Bio { get; set; }
	public bool UpdateBio { get; set; }
	public string? ProfilePhotoUrl { get; set; }
	public bool UpdateProfilePhotoUrl { get; set; }
}