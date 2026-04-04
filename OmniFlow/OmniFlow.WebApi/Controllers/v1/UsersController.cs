using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.DTOs.Users;
using OmniFlow.Application.Features.Users.Commands.UpdateProfile;
using OmniFlow.Application.Features.Users.Queries.GetUserProfile;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.WebApi.Controllers.v1;

[Route("api/v1/users")]
public class UsersController : BaseApiController
{
	private readonly IAuthenticatedUserService _authenticatedUserService;

	public UsersController(IAuthenticatedUserService authenticatedUserService)
	{
		_authenticatedUserService = authenticatedUserService;
	}

	[HttpGet("me")]
	[ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public async Task<IActionResult> Me()
	{
		var query = new GetUserProfileQuery
		{
			UserKey = _authenticatedUserService.UserId
		};

		var result = await Mediator.Send(query);
		return Ok(result);
	}

	[HttpGet("{username}")]
	[ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> GetByUsername([FromRoute] string username)
	{
		var query = new GetUserProfileQuery
		{
			UserKey = username
		};

		var result = await Mediator.Send(query);
		return Ok(result);
	}

	[HttpPut("me")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
	public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest request)
	{
		var command = new UpdateProfileCommand
		{
			Bio = request.Bio,
			ProfilePhotoUrl = request.ProfilePhotoUrl
		};

		await Mediator.Send(command);
		return NoContent();
	}
}
