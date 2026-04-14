using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.Features.Blocks.Commands.BlockUser;
using OmniFlow.Application.Features.Blocks.Commands.UnblockUser;
using OmniFlow.Application.Features.Blocks.Queries.GetBlockedUsers;

namespace OmniFlow.WebApi.Controllers.v1;

[Route("api/v1/users")]
public class BlocksController : BaseApiController
{
	[HttpPost("{userId:guid}/block")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status409Conflict)]
	public async Task<IActionResult> Block([FromRoute] Guid userId)
	{
		await Mediator.Send(new BlockUserCommand { UserId = userId });
		return NoContent();
	}

	[HttpDelete("{userId:guid}/block")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public async Task<IActionResult> Unblock([FromRoute] Guid userId)
	{
		await Mediator.Send(new UnblockUserCommand { UserId = userId });
		return NoContent();
	}

	[HttpGet("{userId:guid}/blocked-users")]
	[ProducesResponseType(typeof(OmniFlow.Application.Wrappers.PagedResponse<OmniFlow.Application.DTOs.Blocks.BlockedUserResponse>), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> GetBlockedUsers([FromRoute] Guid userId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
	{
		var query = new GetBlockedUsersQuery
		{
			UserId = userId,
			Parameter = new GetBlockedUsersParameter
			{
				PageNumber = pageNumber,
				PageSize = pageSize
			}
		};

		var result = await Mediator.Send(query);
		return Ok(result);
	}
}