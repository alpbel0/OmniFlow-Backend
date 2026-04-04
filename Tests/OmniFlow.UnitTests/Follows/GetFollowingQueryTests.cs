using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OmniFlow.Application.DTOs.Follows;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.Follows.Queries.GetFollowing;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Application.Mappings;
using OmniFlow.Application.Parameters;
using OmniFlow.Application.Wrappers;
using OmniFlow.Domain.Entities;
using OmniFlow.UnitTests;

namespace OmniFlow.UnitTests.Follows;

public class GetFollowingQueryTests
{
    private readonly Mock<IFollowRepositoryAsync> _followRepositoryMock = new();
    private readonly Mock<IApplicationDbContext> _contextMock = new();
    private readonly Mock<IAuthenticatedUserService> _authenticatedUserServiceMock = new();
    private readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.AddProfile<GeneralProfile>(), NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public async Task Handle_TargetUserNotFound_ShouldThrowEntityNotFoundException()
    {
        var targetUserId = Guid.NewGuid();
        _contextMock.Setup(x => x.Users).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<User>()).Object);

        var handler = new GetFollowingQueryHandler(
            _followRepositoryMock.Object,
            _contextMock.Object,
            _authenticatedUserServiceMock.Object,
            _mapper);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => handler.Handle(new GetFollowingQuery
        {
            UserId = targetUserId,
            Parameter = new GetFollowingParameter
            {
                PageNumber = 1,
                PageSize = 10
            }
        }, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ReturnsFollowingUsersWithFlags()
    {
        var currentUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var followingOneId = Guid.NewGuid();
        var followingTwoId = Guid.NewGuid();

        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(currentUserId.ToString());

        _contextMock.Setup(x => x.Users).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<User>
        {
            new() { Id = targetUserId, Username = "target", Email = "target@example.com" }
        }).Object);

        var followingOne = new User { Id = followingOneId, Username = "alice", Email = "alice@example.com" };
        var followingTwo = new User { Id = followingTwoId, Username = "bob", Email = "bob@example.com" };

        _followRepositoryMock.Setup(x => x.GetFollowingAsync(targetUserId, It.IsAny<RequestParameter>()))
            .ReturnsAsync(new PagedResponse<Follow>(new List<Follow>
            {
                new() { FollowerId = targetUserId, FollowingId = followingOneId, Following = followingOne },
                new() { FollowerId = targetUserId, FollowingId = followingTwoId, Following = followingTwo }
            }, 1, 10, 2));

        _contextMock.Setup(x => x.Follows).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Follow>
        {
            new() { FollowerId = currentUserId, FollowingId = followingOneId }
        }).Object);

        var handler = new GetFollowingQueryHandler(
            _followRepositoryMock.Object,
            _contextMock.Object,
            _authenticatedUserServiceMock.Object,
            _mapper);

        var result = await handler.Handle(new GetFollowingQuery
        {
            UserId = targetUserId,
            Parameter = new GetFollowingParameter
            {
                PageNumber = 1,
                PageSize = 10
            }
        }, CancellationToken.None);

        result.Data.Should().HaveCount(2);
        result.Data[0].Id.Should().Be(followingOneId);
        result.Data[0].Username.Should().Be("alice");
        result.Data[0].IsFollowing.Should().BeTrue();
        result.Data[1].Id.Should().Be(followingTwoId);
        result.Data[1].IsFollowing.Should().BeFalse();
    }
}