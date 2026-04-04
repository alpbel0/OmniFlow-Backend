using MediatR;

namespace OmniFlow.Application.Features.Users.Commands.UpdateProfile;

public class UpdateProfileCommand : IRequest<Unit>
{
	public string? Bio { get; set; }
	public string? ProfilePhotoUrl { get; set; }
}