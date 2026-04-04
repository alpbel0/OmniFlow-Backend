using AutoMapper;
using MediatR;
using Moq;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.Posts.Commands.UpdatePost;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;

namespace OmniFlow.UnitTests.Posts;

public class UpdatePostCommandHandlerTests
{
    private readonly Mock<IPostRepositoryAsync> _postRepositoryMock = new();
    private readonly Mock<IAuthenticatedUserService> _authenticatedUserServiceMock = new();
    private readonly Mock<IMapper> _mapperMock = new();

    [Fact]
    public async Task Handle_PostNotFound_ShouldThrowEntityNotFoundException()
    {
        var command = new UpdatePostCommand { PostId = Guid.NewGuid(), Content = "new" };
        _postRepositoryMock.Setup(x => x.GetByIdWithUserAsync(command.PostId)).ReturnsAsync((Post?)null);

        var handler = new UpdatePostCommandHandler(_postRepositoryMock.Object, _authenticatedUserServiceMock.Object, _mapperMock.Object);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NotOwner_ShouldThrowForbiddenException()
    {
        var command = new UpdatePostCommand { PostId = Guid.NewGuid(), Content = "new" };
        var currentUserId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(currentUserId.ToString());
        _postRepositoryMock.Setup(x => x.GetByIdWithUserAsync(command.PostId)).ReturnsAsync(new Post { Id = command.PostId, UserId = ownerId });

        var handler = new UpdatePostCommandHandler(_postRepositoryMock.Object, _authenticatedUserServiceMock.Object, _mapperMock.Object);

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ValidOwner_ShouldUpdatePostAndReturnUnit()
    {
        var currentUserId = Guid.NewGuid();
        var command = new UpdatePostCommand { PostId = Guid.NewGuid(), Content = "updated", Tags = new List<string> { "x" } };
        var post = new Post { Id = command.PostId, UserId = currentUserId, Content = "old" };

        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(currentUserId.ToString());
        _postRepositoryMock.Setup(x => x.GetByIdWithUserAsync(command.PostId)).ReturnsAsync(post);

        var handler = new UpdatePostCommandHandler(_postRepositoryMock.Object, _authenticatedUserServiceMock.Object, _mapperMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().Be(Unit.Value);
        _mapperMock.Verify(x => x.Map(command, post), Times.Once);
        _postRepositoryMock.Verify(x => x.UpdateAsync(post), Times.Once);
    }
}
