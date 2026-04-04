namespace OmniFlow.Application.Features.Notifications.Queries.GetNotifications;

public class GetNotificationsParameter
{
	public int PageNumber { get; set; } = 1;
	public int PageSize { get; set; } = 10;
	public bool? IsRead { get; set; }
}
