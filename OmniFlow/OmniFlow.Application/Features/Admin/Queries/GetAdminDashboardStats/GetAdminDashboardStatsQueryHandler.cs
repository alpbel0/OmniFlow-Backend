using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.DTOs.Admin;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Application.Features.Admin.Queries.GetAdminDashboardStats;

public sealed class GetAdminDashboardStatsQueryHandler
	: IRequestHandler<GetAdminDashboardStatsQuery, AdminDashboardStatsResponse>
{
	private static readonly TimeZoneInfo IstanbulTimeZone =
		TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");

	private readonly IApplicationDbContext _context;
	private readonly IDateTimeService _dateTimeService;

	public GetAdminDashboardStatsQueryHandler(
		IApplicationDbContext context,
		IDateTimeService dateTimeService)
	{
		_context = context;
		_dateTimeService = dateTimeService;
	}

	public async Task<AdminDashboardStatsResponse> Handle(
		GetAdminDashboardStatsQuery request,
		CancellationToken cancellationToken)
	{
		var (todayStartUtc, weekStartUtc) = GetCalendarBoundariesUtc(_dateTimeService.NowUtc);
		var activeUsers = _context.Users.AsNoTracking().Where(user => user.DeletedAt == null);
		var activeTrips = _context.Trips.AsNoTracking().Where(trip => trip.DeletedAt == null);
		var activePosts = _context.Posts.AsNoTracking().Where(post => post.DeletedAt == null);

		return new AdminDashboardStatsResponse
		{
			TotalUsers = await activeUsers.CountAsync(cancellationToken),
			NewUsersToday = await activeUsers.CountAsync(user => user.CreatedAt >= todayStartUtc, cancellationToken),
			NewUsersThisWeek = await activeUsers.CountAsync(user => user.CreatedAt >= weekStartUtc, cancellationToken),
			TotalTrips = await activeTrips.CountAsync(cancellationToken),
			NewTripsToday = await activeTrips.CountAsync(trip => trip.CreatedAt >= todayStartUtc, cancellationToken),
			TotalPosts = await activePosts.CountAsync(cancellationToken),
			NewPostsToday = await activePosts.CountAsync(post => post.CreatedAt >= todayStartUtc, cancellationToken)
		};
	}

	private static (DateTime TodayStartUtc, DateTime WeekStartUtc) GetCalendarBoundariesUtc(DateTime nowUtc)
	{
		var utc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
		var localNow = TimeZoneInfo.ConvertTimeFromUtc(utc, IstanbulTimeZone);
		var localToday = localNow.Date;
		var daysSinceMonday = ((int)localToday.DayOfWeek + 6) % 7;
		var localWeekStart = localToday.AddDays(-daysSinceMonday);

		return (
			TimeZoneInfo.ConvertTimeToUtc(localToday, IstanbulTimeZone),
			TimeZoneInfo.ConvertTimeToUtc(localWeekStart, IstanbulTimeZone));
	}
}
