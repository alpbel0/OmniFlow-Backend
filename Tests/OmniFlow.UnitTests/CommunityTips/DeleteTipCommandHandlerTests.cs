using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.CommunityTips.Commands.DeleteTip;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Application.Mappings;
using OmniFlow.Domain.Entities;

namespace OmniFlow.UnitTests.CommunityTips;

public class DeleteTipCommandHandlerTests
{
	private readonly Mock<ICommunityTipRepositoryAsync> _tipRepositoryMock = new();
	private readonly Mock<IAuthenticatedUserService> _authenticatedUserServiceMock = new();
	private readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.AddProfile<GeneralProfile>(), NullLoggerFactory.Instance).CreateMapper();

	[Fact]
	public async Task Handle_TipNotFound_ShouldThrowEntityNotFoundException()
	{
		var tipId = Guid.NewGuid();
		_tipRepositoryMock.Setup(x => x.GetByIdAsync(tipId)).ReturnsAsync((CommunityTip?)null);

		var handler = new DeleteTipCommandHandler(
			_tipRepositoryMock.Object,
			_authenticatedUserServiceMock.Object,
			_mapper);

		await Assert.ThrowsAsync<EntityNotFoundException>(() => handler.Handle(new DeleteTipCommand
		{
			TipId = tipId
		}, CancellationToken.None));
	}

	[Fact]
	public async Task Handle_NotOwner_ShouldThrowForbiddenException()
	{
		var tipId = Guid.NewGuid();
		_tipRepositoryMock.Setup(x => x.GetByIdAsync(tipId)).ReturnsAsync(new CommunityTip
		{
			Id = tipId,
			UserId = Guid.NewGuid()
		});
		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());

		var handler = new DeleteTipCommandHandler(
			_tipRepositoryMock.Object,
			_authenticatedUserServiceMock.Object,
			_mapper);

		await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(new DeleteTipCommand
		{
			TipId = tipId
		}, CancellationToken.None));
	}

	[Fact]
	public async Task Handle_ValidDelete_ShouldSoftDeleteTip()
	{
		var tipId = Guid.NewGuid();
		var currentUserId = Guid.NewGuid();
		var tip = new CommunityTip
		{
			Id = tipId,
			UserId = currentUserId,
			Content = "Great bakery"
		};

		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(currentUserId.ToString());
		_tipRepositoryMock.Setup(x => x.GetByIdAsync(tipId)).ReturnsAsync(tip);
		_tipRepositoryMock.Setup(x => x.DeleteAsync(tip)).Returns(Task.CompletedTask);

		var handler = new DeleteTipCommandHandler(
			_tipRepositoryMock.Object,
			_authenticatedUserServiceMock.Object,
			_mapper);

		await handler.Handle(new DeleteTipCommand
		{
			TipId = tipId
		}, CancellationToken.None);

		_tipRepositoryMock.Verify(x => x.DeleteAsync(tip), Times.Once);
	}
}