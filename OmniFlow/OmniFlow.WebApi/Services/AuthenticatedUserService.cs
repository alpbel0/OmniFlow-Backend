using System.Security.Claims;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.WebApi.Services;

public class AuthenticatedUserService : IAuthenticatedUserService
{
	public string UserId { get; }

	public AuthenticatedUserService(IHttpContextAccessor httpContextAccessor)
	{
		UserId = httpContextAccessor.HttpContext?.User
			.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
	}
}
