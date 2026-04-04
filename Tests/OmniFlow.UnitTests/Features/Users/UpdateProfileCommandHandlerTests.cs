using Moq;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.Users.Commands.UpdateProfile;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;

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
			ProfilePhotoUrl = "https://cdn.example.com/new.jpg"
		}, CancellationToken.None);

		await act.Should().ThrowAsync<EntityNotFoundException>()
			.WithMessage($"User with id '{currentUserId}' was not found.");
	}

	[Fact]
	public async Task Handle_ValidCommand_UpdatesBioAndProfilePhotoUrl()
	{
		var currentUserId = Guid.NewGuid();
		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(currentUserId.ToString());

		var user = new User
		{
			Id = currentUserId,
			Username = "alice",
			Email = "alice@example.com",
			Bio = "Old bio",
			ProfilePhotoUrl = "https://cdn.example.com/old.jpg"
		};

		_userRepositoryMock.Setup(x => x.GetByIdAsync(currentUserId)).ReturnsAsync(user);
		_userRepositoryMock.Setup(x => x.UpdateAsync(user)).Returns(Task.CompletedTask);

		var handler = new UpdateProfileCommandHandler(
			_userRepositoryMock.Object,
			_authenticatedUserServiceMock.Object);

		await handler.Handle(new UpdateProfileCommand
		{
			Bio = "  Updated bio  ",
			ProfilePhotoUrl = "  https://cdn.example.com/new.jpg  "
		}, CancellationToken.None);

		user.Bio.Should().Be("Updated bio");
		user.ProfilePhotoUrl.Should().Be("https://cdn.example.com/new.jpg");
		_userRepositoryMock.Verify(x => x.UpdateAsync(user), Times.Once);
	}
}