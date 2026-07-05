using Moq;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.Users.Commands.UpdateProfile;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.UnitTests.Features.Users;

public class UpdateProfileCommandHandlerTests
{
	private readonly Mock<IUserRepositoryAsync> _userRepositoryMock = new();
	private readonly Mock<IAuthenticatedUserService> _authenticatedUserServiceMock = new();

	[Fact]
	public async Task Handle_UserNotFound_ShouldThrowEntityNotFoundException()
	{
		var currentUserId = Guid.NewGuid();
		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(currentUserId.ToString());
		_userRepositoryMock.Setup(x => x.GetByIdAsync(currentUserId)).ReturnsAsync((User?)null);

		var handler = new UpdateProfileCommandHandler(
			_userRepositoryMock.Object,
			_authenticatedUserServiceMock.Object);

		var act = async () => await handler.Handle(new UpdateProfileCommand
		{
			Bio = "Updated bio",
			UpdateBio = true,
			ProfilePhotoUrl = "https://cdn.example.com/new.jpg",
			UpdateProfilePhotoUrl = true,
			Location = "Istanbul, Turkiye",
			UpdateLocation = true,
			LocationLatitude = 41.0082,
			LocationLongitude = 28.9784,
			UpdateLocationCoordinates = true,
			TravelStyles = new List<TravelStyle> { TravelStyle.Cultural },
			UpdateTravelStyles = true
		}, CancellationToken.None);

		await act.Should().ThrowAsync<EntityNotFoundException>()
			.WithMessage($"User with id '{currentUserId}' was not found.");
	}

	[Fact]
	public async Task Handle_ValidCommand_UpdatesProfileFields()
	{
		var currentUserId = Guid.NewGuid();
		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(currentUserId.ToString());

		var user = new User
		{
			Id = currentUserId,
			Username = "alice",
			Email = "alice@example.com",
			Bio = "Old bio",
			ProfilePhotoUrl = "https://cdn.example.com/old.jpg",
			Location = "Old location",
			LocationLatitude = 40,
			LocationLongitude = 29,
			TravelStyles = new List<TravelStyle> { TravelStyle.Relax }
		};

		_userRepositoryMock.Setup(x => x.GetByIdAsync(currentUserId)).ReturnsAsync(user);
		_userRepositoryMock.Setup(x => x.UpdateAsync(user)).Returns(Task.CompletedTask);

		var handler = new UpdateProfileCommandHandler(
			_userRepositoryMock.Object,
			_authenticatedUserServiceMock.Object);

		await handler.Handle(new UpdateProfileCommand
		{
			Bio = "  Updated bio  ",
			UpdateBio = true,
			ProfilePhotoUrl = "  https://cdn.example.com/new.jpg  ",
			UpdateProfilePhotoUrl = true,
			Location = "  Istanbul, Turkiye  ",
			UpdateLocation = true,
			LocationLatitude = 41.0082,
			LocationLongitude = 28.9784,
			UpdateLocationCoordinates = true,
			TravelStyles = new List<TravelStyle>
			{
				TravelStyle.Cultural,
				TravelStyle.Adventure,
				TravelStyle.Cultural
			},
			UpdateTravelStyles = true
		}, CancellationToken.None);

		user.Bio.Should().Be("Updated bio");
		user.ProfilePhotoUrl.Should().Be("https://cdn.example.com/new.jpg");
		user.Location.Should().Be("Istanbul, Turkiye");
		user.LocationLatitude.Should().Be(41.0082);
		user.LocationLongitude.Should().Be(28.9784);
		user.TravelStyles.Should().Equal(TravelStyle.Cultural, TravelStyle.Adventure);
		_userRepositoryMock.Verify(x => x.UpdateAsync(user), Times.Once);
	}

	[Fact]
	public async Task Handle_NullTravelStyles_NormalizesToEmptyList()
	{
		var currentUserId = Guid.NewGuid();
		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(currentUserId.ToString());

		var user = new User
		{
			Id = currentUserId,
			Username = "alice",
			Email = "alice@example.com",
			TravelStyles = new List<TravelStyle> { TravelStyle.Relax }
		};

		_userRepositoryMock.Setup(x => x.GetByIdAsync(currentUserId)).ReturnsAsync(user);
		_userRepositoryMock.Setup(x => x.UpdateAsync(user)).Returns(Task.CompletedTask);

		var handler = new UpdateProfileCommandHandler(
			_userRepositoryMock.Object,
			_authenticatedUserServiceMock.Object);

		await handler.Handle(new UpdateProfileCommand
		{
			TravelStyles = null,
			UpdateTravelStyles = true
		}, CancellationToken.None);

		user.TravelStyles.Should().BeEmpty();
		_userRepositoryMock.Verify(x => x.UpdateAsync(user), Times.Once);
	}

	[Fact]
	public async Task Handle_NullLocationCoordinates_ClearsCoordinates()
	{
		var currentUserId = Guid.NewGuid();
		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(currentUserId.ToString());

		var user = new User
		{
			Id = currentUserId,
			Username = "alice",
			Email = "alice@example.com",
			LocationLatitude = 41.0082,
			LocationLongitude = 28.9784
		};

		_userRepositoryMock.Setup(x => x.GetByIdAsync(currentUserId)).ReturnsAsync(user);
		_userRepositoryMock.Setup(x => x.UpdateAsync(user)).Returns(Task.CompletedTask);

		var handler = new UpdateProfileCommandHandler(
			_userRepositoryMock.Object,
			_authenticatedUserServiceMock.Object);

		await handler.Handle(new UpdateProfileCommand
		{
			LocationLatitude = null,
			LocationLongitude = null,
			UpdateLocationCoordinates = true
		}, CancellationToken.None);

		user.LocationLatitude.Should().BeNull();
		user.LocationLongitude.Should().BeNull();
		_userRepositoryMock.Verify(x => x.UpdateAsync(user), Times.Once);
	}
}
