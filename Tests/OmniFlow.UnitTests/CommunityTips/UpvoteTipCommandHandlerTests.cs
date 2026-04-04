using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.CommunityTips.Commands.UpvoteTip;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Application.Mappings;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;
using OmniFlow.Domain.Exceptions;

namespace OmniFlow.UnitTests.CommunityTips;

public class UpvoteTipCommandHandlerTests
{
	private readonly Mock<ICommunityTipRepositoryAsync> _tipRepositoryMock = new();
	private readonly Mock<IApplicationDbContext> _contextMock = new();
	private readonly Mock<IAuthenticatedUserService> _authenticatedUserServiceMock = new();
	private readonly Mock<IKarmaService> _karmaServiceMock = new();
	private readonly Mock<INotificationService> _notificationServiceMock = new();
	private readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.AddProfile<GeneralProfile>(), NullLoggerFactory.Instance).CreateMapper();

	[Fact]
	public async Task Handle_TipNotFound_ShouldThrowEntityNotFoundException()
	{
		var tipId = Guid.NewGuid();
		_tipRepositoryMock.Setup(x => x.GetByIdAsync(tipId)).ReturnsAsync((CommunityTip?)null);

		var handler = new UpvoteTipCommandHandler(
			_tipRepositoryMock.Object,
			_contextMock.Object,
			_authenticatedUserServiceMock.Object,
			_mapper,
			_karmaServiceMock.Object,
			_notificationServiceMock.Object);

		await Assert.ThrowsAsync<EntityNotFoundException>(() => handler.Handle(new UpvoteTipCommand
		{
			TipId = tipId
		}, CancellationToken.None));
	}

	[Fact]
	public async Task Handle_DuplicateUpvote_ShouldThrowDuplicateUpvoteException()
	{
		var tipId = Guid.NewGuid();
		var currentUserId = Guid.NewGuid();
		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(currentUserId.ToString());

		_tipRepositoryMock.Setup(x => x.GetByIdAsync(tipId)).ReturnsAsync(new CommunityTip
		{
			Id = tipId,
			UserId = Guid.NewGuid(),
			UpvoteCount = 2
		});
		_contextMock.Setup(x => x.TipUpvotes).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<TipUpvote>
		{
			new() { TipId = tipId, UserId = currentUserId }
		}).Object);

		var handler = new UpvoteTipCommandHandler(
			_tipRepositoryMock.Object,
			_contextMock.Object,
			_authenticatedUserServiceMock.Object,
			_mapper,
			_karmaServiceMock.Object,
			_notificationServiceMock.Object);

		await Assert.ThrowsAsync<DuplicateUpvoteException>(() => handler.Handle(new UpvoteTipCommand
		{
			TipId = tipId
		}, CancellationToken.None));
	}

	[Fact]
	public async Task Handle_ValidUpvote_ShouldIncreaseCountAndCreateVote()
	{
		var tipId = Guid.NewGuid();
		var currentUserId = Guid.NewGuid();
		var tip = new CommunityTip
		{
			Id = tipId,
			UserId = Guid.NewGuid(),
			Content = "Great bakery",
			UpvoteCount = 0
		};

		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(currentUserId.ToString());
		_tipRepositoryMock.Setup(x => x.GetByIdAsync(tipId)).ReturnsAsync(tip);
		_contextMock.Setup(x => x.TipUpvotes).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<TipUpvote>()).Object);
		_contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

		var handler = new UpvoteTipCommandHandler(
			_tipRepositoryMock.Object,
			_contextMock.Object,
			_authenticatedUserServiceMock.Object,
			_mapper,
			_karmaServiceMock.Object,
			_notificationServiceMock.Object);

		await handler.Handle(new UpvoteTipCommand
		{
			TipId = tipId
		}, CancellationToken.None);

		tip.UpvoteCount.Should().Be(1);
		_contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
		_karmaServiceMock.Verify(x => x.AwardKarmaAsync(
			tip.UserId,
			currentUserId,
			OmniFlow.Domain.Enums.KarmaEventType.TipUpvoted,
			2,
			tipId,
			OmniFlow.Domain.Enums.KarmaSourceType.Tip), Times.Once);
		_notificationServiceMock.Verify(x => x.CreateNotificationAsync(
			tip.UserId,
			currentUserId,
			NotificationType.TipUpvote,
			tipId,
			NotificationTargetType.Tip), Times.Once);
	}
}