using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.DTOs.CommunityTips;
using OmniFlow.Application.Features.CommunityTips.Commands.CreateTip;
using OmniFlow.Application.Features.CommunityTips.Commands.DeleteTip;
using OmniFlow.Application.Features.CommunityTips.Commands.RemoveUpvoteTip;
using OmniFlow.Application.Features.CommunityTips.Commands.UpvoteTip;
using OmniFlow.Application.Features.CommunityTips.Queries.GetTipsByTrip;

namespace OmniFlow.WebApi.Controllers.v1;

/// <summary>
/// Community tip endpoints for trip-wide and place-specific tips.
/// All endpoints require authentication.
/// </summary>
public class CommunityTipsController : BaseApiController
{
	[HttpGet("/api/v1/trips/{tripId:guid}/tips")]
	[ProducesResponseType(typeof(OmniFlow.Application.Wrappers.PagedResponse<TipResponse>), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> GetByTrip([FromRoute] Guid tripId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
	{
		var query = new GetTipsByTripQuery
		{
			TripId = tripId,
			PageNumber = pageNumber,
			PageSize = pageSize
		};

		var result = await Mediator.Send(query);
		return Ok(result);
	}

	[HttpPost("/api/v1/trips/{tripId:guid}/tips")]
	[ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
	public async Task<IActionResult> Create([FromRoute] Guid tripId, [FromBody] CreateTipRequest request)
	{
		var command = new CreateTipCommand
		{
			TripId = tripId,
			PlaceId = request.PlaceId,
			Content = request.Content
		};

		var tipId = await Mediator.Send(command);
		return CreatedAtAction(nameof(GetByTrip), new { tripId }, tipId);
	}

	[HttpDelete("/api/v1/tips/{id:guid}")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> Delete([FromRoute] Guid id)
	{
		await Mediator.Send(new DeleteTipCommand
		{
			TipId = id
		});

		return NoContent();
	}

	[HttpPost("/api/v1/tips/{id:guid}/upvote")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status409Conflict)]
	public async Task<IActionResult> Upvote([FromRoute] Guid id)
	{
		await Mediator.Send(new UpvoteTipCommand
		{
			TipId = id
		});

		return NoContent();
	}

	/// <summary>Remove upvote from a community tip.</summary>
	[HttpDelete("/api/v1/tips/{id:guid}/upvote")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> RemoveUpvote([FromRoute] Guid id)
	{
		await Mediator.Send(new RemoveUpvoteTipCommand
		{
			TipId = id
		});

		return NoContent();
	}
}