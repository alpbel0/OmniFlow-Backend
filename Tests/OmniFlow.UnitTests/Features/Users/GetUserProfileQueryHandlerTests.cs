using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OmniFlow.Application.DTOs.Users;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.Users.Queries.GetUserProfile;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Application.Mappings;
using OmniFlow.Domain.Entities;
using OmniFlow.UnitTests;

namespace OmniFlow.UnitTests.Features.Users;

public class GetUserProfileQueryHandlerTests
{
	private readonly Mock<IUserRepositoryAsync> _userRepositoryMock = new();
	private readonly Mock<IApplicationDbContext> _contextMock = new();
	private readonly Mock<IAuthenticatedUserService> _authenticatedUserServiceMock = new();
	private readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.AddProfile<GeneralProfile>(), NullLoggerFactory.Instance).CreateMapper();

	[Fact]
	public async Task Handle_UserNotFoundById_ShouldThrowEntityNotFoundException()
	{
		var userId = Guid.NewGuid();
		_userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync((User?)null);

		var handler = new GetUserProfileQueryHandler(
			_userRepositoryMock.Object,
			_contextMock.Object,
			_authenticatedUserServiceMock.Object,
			_mapper);

		var act = async () => await handler.Handle(new GetUserProfileQuery { UserKey = userId.ToString() }, CancellationToken.None);

		await act.Should().ThrowAsync<EntityNotFoundException>()
			.WithMessage($"User with id '{userId}' was not found.");
	}

	[Fact]
	public async Task Handle_UserNotFoundByUsername_ShouldThrowEntityNotFoundException()
	{
		var username = "missing-user";
		_userRepositoryMock.Setup(x => x.GetByUsernameAsync(username)).ReturnsAsync((User?)null);

		var handler = new GetUserProfileQueryHandler(
			_userRepositoryMock.Object,
			_contextMock.Object,
			_authenticatedUserServiceMock.Object,
			_mapper);

		var act = async () => await handler.Handle(new GetUserProfileQuery { UserKey = username }, CancellationToken.None);

		await act.Should().ThrowAsync<EntityNotFoundException>()
			.WithMessage($"User with id '{username}' was not found.");
	}

	[Fact]
	public async Task Handle_ReturnsProfileById_WithCountsAndIsFollowing()
	{
		var currentUserId = Guid.NewGuid();
		var targetUserId = Guid.NewGuid();

		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(currentUserId.ToString());

		var targetUser = new User
		{
			Id = targetUserId,
			Username = "alice",
			Email = "alice@example.com",
			Bio = "Traveler",
			ProfilePhotoUrl = "https://cdn.example.com/alice.jpg",
			KarmaScore = 75,
			FollowersCount = 11,
			FollowingCount = 4,
			IsVerified = true
		};

		_userRepositoryMock.Setup(x => x.GetByIdAsync(targetUserId)).ReturnsAsync(targetUser);

		_contextMock.Setup(x => x.Trips).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Trip>
		{
			new() { Id = Guid.NewGuid(), OwnerId = targetUserId },
			new() { Id = Guid.NewGuid(), OwnerId = targetUserId },
			new() { Id = Guid.NewGuid(), OwnerId = Guid.NewGuid() }
		}).Object);

		_contextMock.Setup(x => x.Posts).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Post>
		{
			new() { Id = Guid.NewGuid(), UserId = targetUserId },
			new() { Id = Guid.NewGuid(), UserId = targetUserId },
			new() { Id = Guid.NewGuid(), UserId = currentUserId }
		}).Object);

		_contextMock.Setup(x => x.Follows).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Follow>
		{
			new() { FollowerId = currentUserId, FollowingId = targetUserId }
		}).Object);
		_contextMock.Setup(x => x.Blocks).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Block>()).Object);

		var handler = new GetUserProfileQueryHandler(
			_userRepositoryMock.Object,
			_contextMock.Object,
			_authenticatedUserServiceMock.Object,
			_mapper);

		var result = await handler.Handle(new GetUserProfileQuery { UserKey = targetUserId.ToString() }, CancellationToken.None);

		result.Id.Should().Be(targetUserId);
		result.Username.Should().Be("alice");
		result.Email.Should().Be("alice@example.com");
		result.TripCount.Should().Be(2);
		result.PostCount.Should().Be(2);
		result.IsFollowing.Should().BeTrue();
	}

	[Fact]
	public async Task Handle_ReturnsProfileByUsername_WithCountsAndIsFollowing()
	{
		var currentUserId = Guid.NewGuid();
		var targetUserId = Guid.NewGuid();

		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(currentUserId.ToString());

		var targetUser = new User
		{
			Id = targetUserId,
			Username = "bob",
			Email = "bob@example.com",
			KarmaScore = 20,
			FollowersCount = 3,
			FollowingCount = 8,
			IsVerified = false
		};

		_userRepositoryMock.Setup(x => x.GetByUsernameAsync("bob")).ReturnsAsync(targetUser);

		_contextMock.Setup(x => x.Trips).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Trip>
		{
			new() { Id = Guid.NewGuid(), OwnerId = targetUserId }
		}).Object);

		_contextMock.Setup(x => x.Posts).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Post>
		{
			new() { Id = Guid.NewGuid(), UserId = targetUserId }
		}).Object);

		_contextMock.Setup(x => x.Follows).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Follow>
		{
			new() { FollowerId = currentUserId, FollowingId = targetUserId }
		}).Object);
		_contextMock.Setup(x => x.Blocks).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Block>()).Object);

		var handler = new GetUserProfileQueryHandler(
			_userRepositoryMock.Object,
			_contextMock.Object,
			_authenticatedUserServiceMock.Object,
			_mapper);

		var result = await handler.Handle(new GetUserProfileQuery { UserKey = "bob" }, CancellationToken.None);

		result.Id.Should().Be(targetUserId);
		result.Username.Should().Be("bob");
		result.IsFollowing.Should().BeTrue();
		result.TripCount.Should().Be(1);
		result.PostCount.Should().Be(1);
	}

	[Fact]
	public async Task Handle_WhenViewerAndTargetAreBlocked_ShouldReturnProfileWithMetricsZeroed()
	{
		var currentUserId = Guid.NewGuid();
		var targetUserId = Guid.NewGuid();

		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(currentUserId.ToString());

		var targetUser = new User
		{
			Id = targetUserId,
			Username = "blocked-target",
			Email = "blocked-target@example.com",
			KarmaScore = 100,
			FollowersCount = 50,
			FollowingCount = 25
		};

		_userRepositoryMock.Setup(x => x.GetByUsernameAsync("blocked-target")).ReturnsAsync(targetUser);
		_contextMock.Setup(x => x.Trips).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Trip>
		{
			new() { Id = Guid.NewGuid(), OwnerId = targetUserId }
		}).Object);
		_contextMock.Setup(x => x.Posts).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Post>
		{
			new() { Id = Guid.NewGuid(), UserId = targetUserId }
		}).Object);
		_contextMock.Setup(x => x.Follows).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Follow>()).Object);
		_contextMock.Setup(x => x.Blocks).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Block>
		{
			new() { BlockerId = currentUserId, BlockedUserId = targetUserId }
		}).Object);

		var handler = new GetUserProfileQueryHandler(
			_userRepositoryMock.Object,
			_contextMock.Object,
			_authenticatedUserServiceMock.Object,
			_mapper);

		var result = await handler.Handle(new GetUserProfileQuery { UserKey = "blocked-target" }, CancellationToken.None);

		result.Id.Should().Be(targetUserId);
		result.Username.Should().Be("blocked-target");
		result.IsBlocked.Should().BeTrue();
		result.IsBlockedByMe.Should().BeTrue();
		result.FollowersCount.Should().Be(0);
		result.FollowingCount.Should().Be(0);
		result.TripCount.Should().Be(0);
		result.PostCount.Should().Be(0);
		result.KarmaScore.Should().Be(0);
		result.IsFollowing.Should().BeFalse();
	}
}