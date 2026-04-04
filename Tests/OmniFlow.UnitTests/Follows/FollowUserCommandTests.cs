using Moq;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.Follows.Commands.FollowUser;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;
using OmniFlow.Domain.Exceptions;
using OmniFlow.UnitTests;

namespace OmniFlow.UnitTests.Follows;

public class FollowUserCommandTests
{
    private readonly Mock<IFollowRepositoryAsync> _followRepositoryMock = new();
    private readonly Mock<IApplicationDbContext> _contextMock = new();
    private readonly Mock<IAuthenticatedUserService> _authenticatedUserServiceMock = new();
    private readonly Mock<INotificationService> _notificationServiceMock = new();

    [Fact]
    public async Task Handle_SelfFollow_ShouldThrowSelfFollowException()
    {
        var userId = Guid.NewGuid();
        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());

        var handler = new FollowUserCommandHandler(
            _followRepositoryMock.Object,
            _contextMock.Object,
            _authenticatedUserServiceMock.Object,
            _notificationServiceMock.Object);

        await Assert.ThrowsAsync<SelfFollowException>(() => handler.Handle(new FollowUserCommand { UserId = userId }, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AlreadyFollowing_ShouldIgnoreAndNotCreateDuplicate()
    {
        var followerId = Guid.NewGuid();
        var followingId = Guid.NewGuid();

        _contextMock.Setup(x => x.Users).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<User>
        {
            new() { Id = followerId, Username = "follower", Email = "follower@example.com" },
            new() { Id = followingId, Username = "following", Email = "following@example.com" }
        }).Object);

        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(followerId.ToString());
        _followRepositoryMock.Setup(x => x.IsFollowingAsync(followerId, followingId)).ReturnsAsync(true);

        var handler = new FollowUserCommandHandler(
            _followRepositoryMock.Object,
            _contextMock.Object,
            _authenticatedUserServiceMock.Object,
            _notificationServiceMock.Object);

        var result = await handler.Handle(new FollowUserCommand { UserId = followingId }, CancellationToken.None);

        result.Should().Be(MediatR.Unit.Value);
    }

    [Fact]
    public async Task Handle_ValidFollow_ShouldCreateFollowAndIncrementCounters()
    {
        var followerId = Guid.NewGuid();
        var followingId = Guid.NewGuid();

        var follower = new User { Id = followerId, Username = "follower", Email = "follower@example.com", FollowingCount = 0 };
        var following = new User { Id = followingId, Username = "following", Email = "following@example.com", FollowersCount = 0 };

        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(followerId.ToString());
        _followRepositoryMock.Setup(x => x.IsFollowingAsync(followerId, followingId)).ReturnsAsync(false);

        _contextMock.Setup(x => x.Users).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<User>
        {
            follower,
            following
        }).Object);
        _contextMock.Setup(x => x.Follows).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Follow>()).Object);
        _contextMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        var handler = new FollowUserCommandHandler(
            _followRepositoryMock.Object,
            _contextMock.Object,
            _authenticatedUserServiceMock.Object,
            _notificationServiceMock.Object);

        var result = await handler.Handle(new FollowUserCommand { UserId = followingId }, CancellationToken.None);

        result.Should().Be(MediatR.Unit.Value);
        follower.FollowingCount.Should().Be(1);
        following.FollowersCount.Should().Be(1);
        _contextMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _notificationServiceMock.Verify(x => x.CreateNotificationAsync(
            followingId,
            followerId,
            NotificationType.Follow,
            null,
            null), Times.Once);
    }
}