namespace OmniFlow.Application.DTOs.Admin;

public sealed class AdminDashboardStatsResponse
{
	public int TotalUsers { get; init; }
	public int NewUsersToday { get; init; }
	public int NewUsersThisWeek { get; init; }
	public int TotalTrips { get; init; }
	public int NewTripsToday { get; init; }
	public int TotalPosts { get; init; }
	public int NewPostsToday { get; init; }
}
