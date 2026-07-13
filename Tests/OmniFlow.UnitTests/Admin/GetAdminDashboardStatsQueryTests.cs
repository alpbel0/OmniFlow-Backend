using Moq;
using OmniFlow.Application.Features.Admin.Queries.GetAdminDashboardStats;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;

namespace OmniFlow.UnitTests.Admin;

public class GetAdminDashboardStatsQueryTests
{
	[Fact]
	public async Task Handle_UsesIstanbulCalendarAndExcludesSoftDeletedRows()
	{
		var beforeMondayInIstanbul = new DateTime(2026, 7, 12, 20, 59, 59, DateTimeKind.Utc);
		var mondayStartInIstanbul = new DateTime(2026, 7, 12, 21, 0, 0, DateTimeKind.Utc);
		var context = new Mock<IApplicationDbContext>();
		context.Setup(x => x.Users).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<User>
		{
			new() { Id = Guid.NewGuid(), Username = "old", Email = "old@example.com", CreatedAt = beforeMondayInIstanbul },
			new() { Id = Guid.NewGuid(), Username = "new", Email = "new@example.com", CreatedAt = mondayStartInIstanbul },
			new() { Id = Guid.NewGuid(), Username = "deleted", Email = "deleted@example.com", CreatedAt = mondayStartInIstanbul, DeletedAt = mondayStartInIstanbul }
		}).Object);
		context.Setup(x => x.Trips).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Trip>
		{
			new() { Id = Guid.NewGuid(), CreatedAt = mondayStartInIstanbul },
			new() { Id = Guid.NewGuid(), CreatedAt = mondayStartInIstanbul, DeletedAt = mondayStartInIstanbul }
		}).Object);
		context.Setup(x => x.Posts).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Post>
		{
			new() { Id = Guid.NewGuid(), CreatedAt = beforeMondayInIstanbul },
			new() { Id = Guid.NewGuid(), CreatedAt = mondayStartInIstanbul }
		}).Object);
		var clock = new Mock<IDateTimeService>();
		clock.Setup(x => x.NowUtc).Returns(new DateTime(2026, 7, 13, 0, 30, 0, DateTimeKind.Utc));
		var handler = new GetAdminDashboardStatsQueryHandler(context.Object, clock.Object);

		var result = await handler.Handle(new GetAdminDashboardStatsQuery(), CancellationToken.None);

		result.TotalUsers.Should().Be(2);
		result.NewUsersToday.Should().Be(1);
		result.NewUsersThisWeek.Should().Be(1);
		result.TotalTrips.Should().Be(1);
		result.NewTripsToday.Should().Be(1);
		result.TotalPosts.Should().Be(2);
		result.NewPostsToday.Should().Be(1);
	}
}
