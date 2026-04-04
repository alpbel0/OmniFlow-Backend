using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.DTOs.Notifications;
using OmniFlow.Application.Features.Notifications.Commands.MarkAllAsRead;
using OmniFlow.Application.Features.Notifications.Commands.MarkAsRead;
using OmniFlow.Application.Features.Notifications.Queries.GetNotifications;
using OmniFlow.Application.Features.Notifications.Queries.GetUnreadCount;
using OmniFlow.Application.Wrappers;

namespace OmniFlow.WebApi.Controllers.v1;

public class NotificationsController : BaseApiController
{
	[HttpGet]
	[ProducesResponseType(typeof(PagedResponse<NotificationResponse>), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public async Task<IActionResult> GetNotifications([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] bool? isRead = null)
	{
		var result = await Mediator.Send(new GetNotificationsQuery
		{
			Parameter = new GetNotificationsParameter
			{
				PageNumber = pageNumber,
				PageSize = pageSize,
				IsRead = isRead
			}
		});

		return Ok(result);
	}

	[HttpGet("unread-count")]
	[ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public async Task<IActionResult> GetUnreadCount()
	{
		var result = await Mediator.Send(new GetUnreadCountQuery());
		return Ok(result);
	}

	[HttpPost("{id:guid}/read")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> MarkAsRead([FromRoute] Guid id)
	{
		await Mediator.Send(new MarkAsReadCommand { NotificationId = id });
		return NoContent();
	}

	[HttpPost("read-all")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public async Task<IActionResult> MarkAllAsRead()
	{
		await Mediator.Send(new MarkAllAsReadCommand());
		return NoContent();
	}
}
