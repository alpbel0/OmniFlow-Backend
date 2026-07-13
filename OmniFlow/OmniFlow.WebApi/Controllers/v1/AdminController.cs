using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.DTOs.Admin;
using OmniFlow.Application.Features.Admin.Commands.AdminDeletePost;
using OmniFlow.Application.Features.Admin.Commands.SetUserSuspended;
using OmniFlow.Application.Features.Admin.Queries.GetAdminPosts;
using OmniFlow.Application.Features.Admin.Queries.GetAdminDashboardStats;
using OmniFlow.Application.Features.Admin.Queries.GetAdminUsers;
using OmniFlow.Application.Wrappers;
using OmniFlow.Domain.Enums;

namespace OmniFlow.WebApi.Controllers.v1;

[Route("api/v1/admin")]
[Authorize(Roles = nameof(Roles.Admin))]
public class AdminController : BaseApiController
{
	[HttpGet("stats")]
	[ProducesResponseType(typeof(AdminDashboardStatsResponse), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	public async Task<IActionResult> GetStats()
	{
		return Ok(await Mediator.Send(new GetAdminDashboardStatsQuery()));
	}

	[HttpGet("posts")]
	[ProducesResponseType(typeof(PagedResponse<AdminPostListItemResponse>), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	public async Task<IActionResult> GetPosts(
		[FromQuery] int pageNumber = 1,
		[FromQuery] int pageSize = 20,
		[FromQuery] string? search = null)
	{
		var result = await Mediator.Send(new GetAdminPostsQuery
		{
			PageNumber = pageNumber,
			PageSize = pageSize,
			Search = search
		});

		return Ok(result);
	}

	[HttpDelete("posts/{id:guid}")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> DeletePost([FromRoute] Guid id)
	{
		await Mediator.Send(new AdminDeletePostCommand { PostId = id });
		return NoContent();
	}

	[HttpGet("users")]
	[ProducesResponseType(typeof(PagedResponse<AdminUserListItemResponse>), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	public async Task<IActionResult> GetUsers(
		[FromQuery] int pageNumber = 1,
		[FromQuery] int pageSize = 20,
		[FromQuery] string? search = null)
	{
		var result = await Mediator.Send(new GetAdminUsersQuery
		{
			PageNumber = pageNumber,
			PageSize = pageSize,
			Search = search
		});

		return Ok(result);
	}

	[HttpPost("users/{id:guid}/suspend")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> SuspendUser([FromRoute] Guid id)
	{
		await Mediator.Send(new SetUserSuspendedCommand
		{
			UserId = id,
			IsSuspended = true
		});
		return NoContent();
	}

	[HttpDelete("users/{id:guid}/suspend")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> UnsuspendUser([FromRoute] Guid id)
	{
		await Mediator.Send(new SetUserSuspendedCommand
		{
			UserId = id,
			IsSuspended = false
		});
		return NoContent();
	}
}
