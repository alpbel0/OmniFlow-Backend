using MediatR;
using OmniFlow.Application.DTOs.Users;

namespace OmniFlow.Application.Features.Users.Queries.GetUserProfile;

public class GetUserProfileQuery : IRequest<UserProfileResponse>
{
	public string UserKey { get; set; } = string.Empty;
}
