using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.CommunityTips.Commands.CreateTip;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Application.Mappings;
using OmniFlow.Domain.Entities;

namespace OmniFlow.UnitTests.CommunityTips;

public class CreateTipCommandHandlerTests
{
	private readonly Mock<ICommunityTipRepositoryAsync> _tipRepositoryMock = new();
	private readonly Mock<IApplicationDbContext> _contextMock = new();
	private readonly Mock<IAuthenticatedUserService> _authenticatedUserServiceMock = new();
	private readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.AddProfile<GeneralProfile>(), NullLoggerFactory.Instance).CreateMapper();

	[Fact]
	public async Task Handle_TripNotFound_ShouldThrowEntityNotFoundException()
	{
		var tripId = Guid.NewGuid();
		_contextMock.Setup(x => x.Trips).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Trip>()).Object);

		var handler = new CreateTipCommandHandler(
			_tipRepositoryMock.Object,
			_contextMock.Object,
			_authenticatedUserServiceMock.Object,
			_mapper);

		await Assert.ThrowsAsync<EntityNotFoundException>(() => handler.Handle(new CreateTipCommand
		{
			TripId = tripId,
			Content = "Great stop"
		}, CancellationToken.None));
	}

	[Fact]
	public async Task Handle_ValidCommand_ShouldCreateTipAndTrimContent()
	{
		var currentUserId = Guid.NewGuid();
		var tripId = Guid.NewGuid();
		var placeId = Guid.NewGuid();
		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(currentUserId.ToString());

		_contextMock.Setup(x => x.Trips).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Trip>
		{
			new() { Id = tripId }
		}).Object);
		_contextMock.Setup(x => x.Places).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Place>
		{
			new() { Id = placeId, Name = "Bakery", City = "Antalya", Country = "Turkey" }
		}).Object);

		CommunityTip? createdTip = null;
		_tipRepositoryMock.Setup(x => x.AddAsync(It.IsAny<CommunityTip>()))
			.ReturnsAsync((CommunityTip tip) =>
			{
				createdTip = tip;
				return tip;
			});

		var handler = new CreateTipCommandHandler(
			_tipRepositoryMock.Object,
			_contextMock.Object,
			_authenticatedUserServiceMock.Object,
			_mapper);

		var result = await handler.Handle(new CreateTipCommand
		{
			TripId = tripId,
			PlaceId = placeId,
			Content = "  Visit the bakery before 8am  "
		}, CancellationToken.None);

		result.Should().Be(createdTip!.Id);
		createdTip.TripId.Should().Be(tripId);
		createdTip.PlaceId.Should().Be(placeId);
		createdTip.UserId.Should().Be(currentUserId);
		createdTip.Content.Should().Be("Visit the bakery before 8am");
	}
}