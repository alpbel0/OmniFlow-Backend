using Moq;
using OmniFlow.Application.Features.Blocks.Commands.UnblockUser;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.UnitTests;

namespace OmniFlow.UnitTests.Blocks;

public class UnblockUserCommandTests
{
	private readonly Mock<IApplicationDbContext> _contextMock = new();
	private readonly Mock<IAuthenticatedUserService> _authenticatedUserServiceMock = new();

	[Fact]
	public async Task Handle_NoExistingBlock_ShouldIgnore()
	{
		var blockerId = Guid.NewGuid();
		var blockedUserId = Guid.NewGuid();

		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(blockerId.ToString());
		_contextMock.Setup(x => x.Blocks).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Block>()).Object);

		var handler = new UnblockUserCommandHandler(_contextMock.Object, _authenticatedUserServiceMock.Object);

		var result = await handler.Handle(new UnblockUserCommand { UserId = blockedUserId }, CancellationToken.None);

		result.Should().Be(MediatR.Unit.Value);
		_contextMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
	}

	[Fact]
	public async Task Handle_ExistingBlock_ShouldRemoveAndSave()
	{
		var blockerId = Guid.NewGuid();
		var blockedUserId = Guid.NewGuid();

		var blocks = new List<Block>
		{
			new() { BlockerId = blockerId, BlockedUserId = blockedUserId }
		};

		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(blockerId.ToString());
		_contextMock.Setup(x => x.Blocks).Returns(MockDbSetHelper.CreateAsyncMockDbSet(blocks).Object);
		_contextMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

		var handler = new UnblockUserCommandHandler(_contextMock.Object, _authenticatedUserServiceMock.Object);

		var result = await handler.Handle(new UnblockUserCommand { UserId = blockedUserId }, CancellationToken.None);

		result.Should().Be(MediatR.Unit.Value);
		_contextMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
	}
}