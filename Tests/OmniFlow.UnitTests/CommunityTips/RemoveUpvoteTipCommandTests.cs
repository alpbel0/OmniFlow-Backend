using MediatR;
using Moq;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.CommunityTips.Commands.RemoveUpvoteTip;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.UnitTests.CommunityTips;

public class RemoveUpvoteTipCommandHandlerTests
{
	private readonly Mock<ICommunityTipRepositoryAsync> _tipRepositoryMock = new();
	private readonly Mock<IApplicationDbContext> _contextMock = new();
	private readonly Mock<IAuthenticatedUserService> _authenticatedUserServiceMock = new();
	private readonly Mock<IKarmaService> _karmaServiceMock = new();

	[Fact]
	public async Task Handle_TipNotFound_ShouldThrowEntityNotFoundException()
	{
		var command = new RemoveUpvoteTipCommand { TipId = Guid.NewGuid() };
		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
		_tipRepositoryMock.Setup(x => x.GetByIdAsync(command.TipId)).ReturnsAsync((CommunityTip?)null);

		var handler = new RemoveUpvoteTipCommandHandler(_tipRepositoryMock.Object, _contextMock.Object, _authenticatedUserServiceMock.Object, _karmaServiceMock.Object);

		await Assert.ThrowsAsync<EntityNotFoundException>(() => handler.Handle(command, CancellationToken.None));
	}

	[Fact]
	public async Task Handle_UpvoteNotFound_ShouldThrowEntityNotFoundException()
	{
		var userId = Guid.NewGuid();
		var tipId = Guid.NewGuid();
		var tip = new CommunityTip { Id = tipId, UserId = Guid.NewGuid(), UpvoteCount = 5 };

		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
		_tipRepositoryMock.Setup(x => x.GetByIdAsync(tipId)).ReturnsAsync(tip);

		var mockSet = MockDbSetHelper.CreateMockDbSet(new List<TipUpvote>());
		mockSet.Setup(x => x.FindAsync(new object[] { tipId, userId }, default)).ReturnsAsync((TipUpvote?)null);
		_contextMock.Setup(x => x.TipUpvotes).Returns(mockSet.Object);

		var handler = new RemoveUpvoteTipCommandHandler(_tipRepositoryMock.Object, _contextMock.Object, _authenticatedUserServiceMock.Object, _karmaServiceMock.Object);

		await Assert.ThrowsAsync<EntityNotFoundException>(() => handler.Handle(new RemoveUpvoteTipCommand { TipId = tipId }, CancellationToken.None));
	}

	[Fact]
	public async Task Handle_ValidRemoveUpvote_ShouldDecrementCountAndRevokeKarma()
	{
		var userId = Guid.NewGuid();
		var tipId = Guid.NewGuid();
		var tipOwnerId = Guid.NewGuid();
		var tip = new CommunityTip { Id = tipId, UserId = tipOwnerId, UpvoteCount = 10 };

		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
		_tipRepositoryMock.Setup(x => x.GetByIdAsync(tipId)).ReturnsAsync(tip);

		var existingUpvote = new TipUpvote { TipId = tipId, UserId = userId };
		var mockSet = MockDbSetHelper.CreateMockDbSet(new List<TipUpvote> { existingUpvote });
		mockSet.Setup(x => x.FindAsync(new object[] { tipId, userId }, default)).ReturnsAsync(existingUpvote);
		_contextMock.Setup(x => x.TipUpvotes).Returns(mockSet.Object);
		_contextMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

		var handler = new RemoveUpvoteTipCommandHandler(_tipRepositoryMock.Object, _contextMock.Object, _authenticatedUserServiceMock.Object, _karmaServiceMock.Object);

		var result = await handler.Handle(new RemoveUpvoteTipCommand { TipId = tipId }, CancellationToken.None);

		result.Should().Be(Unit.Value);
		tip.UpvoteCount.Should().Be(9);
		mockSet.Verify(x => x.Remove(existingUpvote), Times.Once);
		_karmaServiceMock.Verify(x => x.RevokeKarmaAsync(
			tipOwnerId,
			userId,
			KarmaEventType.TipUpvoted,
			tipId,
			KarmaSourceType.Tip), Times.Once);
	}

	[Fact]
	public async Task Handle_UpvoteCountDoesNotGoBelowZero()
	{
		var userId = Guid.NewGuid();
		var tipId = Guid.NewGuid();
		var tip = new CommunityTip { Id = tipId, UserId = Guid.NewGuid(), UpvoteCount = 0 };

		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
		_tipRepositoryMock.Setup(x => x.GetByIdAsync(tipId)).ReturnsAsync(tip);

		var existingUpvote = new TipUpvote { TipId = tipId, UserId = userId };
		var mockSet = MockDbSetHelper.CreateMockDbSet(new List<TipUpvote> { existingUpvote });
		mockSet.Setup(x => x.FindAsync(new object[] { tipId, userId }, default)).ReturnsAsync(existingUpvote);
		_contextMock.Setup(x => x.TipUpvotes).Returns(mockSet.Object);
		_contextMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

		var handler = new RemoveUpvoteTipCommandHandler(_tipRepositoryMock.Object, _contextMock.Object, _authenticatedUserServiceMock.Object, _karmaServiceMock.Object);

		await handler.Handle(new RemoveUpvoteTipCommand { TipId = tipId }, CancellationToken.None);

		tip.UpvoteCount.Should().Be(0); // Clamped at 0
	}
}