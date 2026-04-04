using MediatR;
using Moq;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.Posts.Commands.DeletePost;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;

namespace OmniFlow.UnitTests.Posts;

public class DeletePostCommandHandlerTests
{
    private readonly Mock<IPostRepositoryAsync> _postRepositoryMock = new();
    private readonly Mock<IAuthenticatedUserService> _authenticatedUserServiceMock = new();

    [Fact]
    public async Task Handle_PostNotFound_ShouldThrowEntityNotFoundException()
    {
        var command = new DeletePostCommand { PostId = Guid.NewGuid() };
        _postRepositoryMock.Setup(x => x.GetByIdWithUserAsync(command.PostId)).ReturnsAsync((Post?)null);

        var handler = new DeletePostCommandHandler(_postRepositoryMock.Object, _authenticatedUserServiceMock.Object);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NotOwner_ShouldThrowForbiddenException()
    {
        var command = new DeletePostCommand { PostId = Guid.NewGuid() };
        var currentUserId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(currentUserId.ToString());
        _postRepositoryMock.Setup(x => x.GetByIdWithUserAsync(command.PostId)).ReturnsAsync(new Post { Id = command.PostId, UserId = ownerId });

        var handler = new DeletePostCommandHandler(_postRepositoryMock.Object, _authenticatedUserServiceMock.Object);

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ValidOwner_ShouldSoftDeleteAndReturnUnit()
    {
        var currentUserId = Guid.NewGuid();
        var command = new DeletePostCommand { PostId = Guid.NewGuid() };
        var post = new Post { Id = command.PostId, UserId = currentUserId };

        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(currentUserId.ToString());
        _postRepositoryMock.Setup(x => x.GetByIdWithUserAsync(command.PostId)).ReturnsAsync(post);

        var handler = new DeletePostCommandHandler(_postRepositoryMock.Object, _authenticatedUserServiceMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().Be(Unit.Value);
        _postRepositoryMock.Verify(x => x.DeleteAsync(post), Times.Once);
    }
}
