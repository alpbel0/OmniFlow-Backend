using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.Features.Follows.Commands.FollowUser;
using OmniFlow.Application.Features.Follows.Commands.UnfollowUser;
using OmniFlow.Application.Features.Follows.Queries.GetFollowers;
using OmniFlow.Application.Features.Follows.Queries.GetFollowing;

namespace OmniFlow.WebApi.Controllers.v1;

[Route("api/v1/users")]
public class FollowsController : BaseApiController
{
	[HttpPost("{userId:guid}/follow")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status409Conflict)]
	public async Task<IActionResult> Follow([FromRoute] Guid userId)
	{
		await Mediator.Send(new FollowUserCommand { UserId = userId });
		return NoContent();
	}

	[HttpDelete("{userId:guid}/follow")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public async Task<IActionResult> Unfollow([FromRoute] Guid userId)
	{
		await Mediator.Send(new UnfollowUserCommand { UserId = userId });
		return NoContent();
	}

	[HttpGet("{userId:guid}/followers")]
	[ProducesResponseType(typeof(OmniFlow.Application.Wrappers.PagedResponse<OmniFlow.Application.DTOs.Follows.FollowUserResponse>), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> GetFollowers([FromRoute] Guid userId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
	{
		var query = new GetFollowersQuery
		{
			UserId = userId,
			Parameter = new OmniFlow.Application.Features.Follows.Queries.GetFollowers.GetFollowersParameter
			{
				PageNumber = pageNumber,
				PageSize = pageSize
			}
		};

		var result = await Mediator.Send(query);
		return Ok(result);
	}

	[HttpGet("{userId:guid}/following")]
	[ProducesResponseType(typeof(OmniFlow.Application.Wrappers.PagedResponse<OmniFlow.Application.DTOs.Follows.FollowUserResponse>), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> GetFollowing([FromRoute] Guid userId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
	{
		var query = new GetFollowingQuery
		{
			UserId = userId,
			Parameter = new OmniFlow.Application.Features.Follows.Queries.GetFollowing.GetFollowingParameter
			{
				PageNumber = pageNumber,
				PageSize = pageSize
			}
		};

		var result = await Mediator.Send(query);
		return Ok(result);
	}
}
