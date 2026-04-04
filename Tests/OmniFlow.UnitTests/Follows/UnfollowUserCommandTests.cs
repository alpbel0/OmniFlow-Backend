using Moq;
using OmniFlow.Application.Features.Follows.Commands.UnfollowUser;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;
using OmniFlow.UnitTests;

namespace OmniFlow.UnitTests.Follows;

public class UnfollowUserCommandTests
{
    private readonly Mock<IFollowRepositoryAsync> _followRepositoryMock = new();
    private readonly Mock<IApplicationDbContext> _contextMock = new();
    private readonly Mock<IAuthenticatedUserService> _authenticatedUserServiceMock = new();

    [Fact]
    public async Task Handle_NoExistingFollow_ShouldIgnore()
    {
        var followerId = Guid.NewGuid();
        var followingId = Guid.NewGuid();

        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(followerId.ToString());
        _contextMock.Setup(x => x.Users).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<User>
        {
            new() { Id = followerId, Username = "follower", Email = "follower@example.com" },
            new() { Id = followingId, Username = "following", Email = "following@example.com" }
        }).Object);
        _contextMock.Setup(x => x.Follows).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Follow>()).Object);

        var handler = new UnfollowUserCommandHandler(
            _contextMock.Object,
            _authenticatedUserServiceMock.Object);

        var result = await handler.Handle(new UnfollowUserCommand { UserId = followingId }, CancellationToken.None);

        result.Should().Be(MediatR.Unit.Value);
        _contextMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task Handle_ExistingFollow_ShouldRemoveAndDecrementCounters()
    {
        var followerId = Guid.NewGuid();
        var followingId = Guid.NewGuid();

        var follower = new User { Id = followerId, Username = "follower", Email = "follower@example.com", FollowingCount = 1 };
        var following = new User { Id = followingId, Username = "following", Email = "following@example.com", FollowersCount = 1 };
        var follow = new Follow
        {
            FollowerId = followerId,
            FollowingId = followingId,
            Follower = follower,
            Following = following
        };

        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(followerId.ToString());
        _contextMock.Setup(x => x.Users).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<User>
        {
            follower,
            following
        }).Object);
        _contextMock.Setup(x => x.Follows).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Follow> { follow }).Object);
        _contextMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        var handler = new UnfollowUserCommandHandler(
            _contextMock.Object,
            _authenticatedUserServiceMock.Object);

        var result = await handler.Handle(new UnfollowUserCommand { UserId = followingId }, CancellationToken.None);

        result.Should().Be(MediatR.Unit.Value);
        follower.FollowingCount.Should().Be(0);
        following.FollowersCount.Should().Be(0);
        _contextMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }
}