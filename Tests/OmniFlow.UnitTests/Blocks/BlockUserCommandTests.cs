using Moq;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.Blocks.Commands.BlockUser;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Exceptions;
using OmniFlow.UnitTests;

namespace OmniFlow.UnitTests.Blocks;

public class BlockUserCommandTests
{
	private readonly Mock<IApplicationDbContext> _contextMock = new();
	private readonly Mock<IAuthenticatedUserService> _authenticatedUserServiceMock = new();

	[Fact]
	public async Task Handle_SelfBlock_ShouldThrowSelfBlockException()
	{
		var userId = Guid.NewGuid();
		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
		_contextMock.Setup(x => x.Users).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<User>
		{
			new() { Id = userId, Username = "user", Email = "user@example.com" }
		}).Object);

		var handler = new BlockUserCommandHandler(_contextMock.Object, _authenticatedUserServiceMock.Object);

		await Assert.ThrowsAsync<SelfBlockException>(() =>
			handler.Handle(new BlockUserCommand { UserId = userId }, CancellationToken.None));
	}

	[Fact]
	public async Task Handle_TargetUserMissing_ShouldThrowEntityNotFoundException()
	{
		var currentUserId = Guid.NewGuid();
		var missingUserId = Guid.NewGuid();

		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(currentUserId.ToString());
		_contextMock.Setup(x => x.Users).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<User>
		{
			new() { Id = currentUserId, Username = "current", Email = "current@example.com" }
		}).Object);
		_contextMock.Setup(x => x.Blocks).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Block>()).Object);

		var handler = new BlockUserCommandHandler(_contextMock.Object, _authenticatedUserServiceMock.Object);

		await Assert.ThrowsAsync<EntityNotFoundException>(() =>
			handler.Handle(new BlockUserCommand { UserId = missingUserId }, CancellationToken.None));
	}

	[Fact]
	public async Task Handle_AlreadyBlocked_ShouldNoOp()
	{
		var currentUserId = Guid.NewGuid();
		var targetUserId = Guid.NewGuid();

		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(currentUserId.ToString());
		_contextMock.Setup(x => x.Users).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<User>
		{
			new() { Id = currentUserId, Username = "current", Email = "current@example.com" },
			new() { Id = targetUserId, Username = "target", Email = "target@example.com" }
		}).Object);
		_contextMock.Setup(x => x.Blocks).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Block>
		{
			new() { BlockerId = currentUserId, BlockedUserId = targetUserId }
		}).Object);

		var handler = new BlockUserCommandHandler(_contextMock.Object, _authenticatedUserServiceMock.Object);

		var result = await handler.Handle(new BlockUserCommand { UserId = targetUserId }, CancellationToken.None);

		result.Should().Be(MediatR.Unit.Value);
		_contextMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
	}

	[Fact]
	public async Task Handle_ValidBlock_ShouldCreateBlockAndCleanupMutualFollows()
	{
		var currentUserId = Guid.NewGuid();
		var targetUserId = Guid.NewGuid();

		var currentUser = new User
		{
			Id = currentUserId,
			Username = "current",
			Email = "current@example.com",
			FollowersCount = 1,
			FollowingCount = 1
		};

		var targetUser = new User
		{
			Id = targetUserId,
			Username = "target",
			Email = "target@example.com",
			FollowersCount = 1,
			FollowingCount = 1
		};

		var follows = new List<Follow>
		{
			new() { FollowerId = currentUserId, FollowingId = targetUserId },
			new() { FollowerId = targetUserId, FollowingId = currentUserId }
		};

		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(currentUserId.ToString());
		_contextMock.Setup(x => x.Users).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<User> { currentUser, targetUser }).Object);
		_contextMock.Setup(x => x.Blocks).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Block>()).Object);
		_contextMock.Setup(x => x.Follows).Returns(MockDbSetHelper.CreateAsyncMockDbSet(follows).Object);
		_contextMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

		var handler = new BlockUserCommandHandler(_contextMock.Object, _authenticatedUserServiceMock.Object);

		var result = await handler.Handle(new BlockUserCommand { UserId = targetUserId }, CancellationToken.None);

		result.Should().Be(MediatR.Unit.Value);
		currentUser.FollowersCount.Should().Be(0);
		currentUser.FollowingCount.Should().Be(0);
		targetUser.FollowersCount.Should().Be(0);
		targetUser.FollowingCount.Should().Be(0);
		_contextMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
	}

	[Fact]
	public async Task Handle_ValidBlock_WithNoFollowRelations_ShouldKeepCounters()
	{
		var currentUserId = Guid.NewGuid();
		var targetUserId = Guid.NewGuid();

		var currentUser = new User
		{
			Id = currentUserId,
			Username = "current",
			Email = "current@example.com",
			FollowersCount = 3,
			FollowingCount = 2
		};

		var targetUser = new User
		{
			Id = targetUserId,
			Username = "target",
			Email = "target@example.com",
			FollowersCount = 4,
			FollowingCount = 5
		};

		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(currentUserId.ToString());
		_contextMock.Setup(x => x.Users).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<User> { currentUser, targetUser }).Object);
		_contextMock.Setup(x => x.Blocks).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Block>()).Object);
		_contextMock.Setup(x => x.Follows).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Follow>()).Object);
		_contextMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

		var handler = new BlockUserCommandHandler(_contextMock.Object, _authenticatedUserServiceMock.Object);

		var result = await handler.Handle(new BlockUserCommand { UserId = targetUserId }, CancellationToken.None);

		result.Should().Be(MediatR.Unit.Value);
		currentUser.FollowersCount.Should().Be(3);
		currentUser.FollowingCount.Should().Be(2);
		targetUser.FollowersCount.Should().Be(4);
		targetUser.FollowingCount.Should().Be(5);
		_contextMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
	}
}