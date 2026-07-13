using AutoMapper;
using Moq;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.Posts.Commands.CreatePost;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.UnitTests.Posts;

public class CreatePostCommandValidatorTests
{
	private readonly CreatePostCommandValidator _validator = new();

	[Fact]
	public void Validate_ValidPhotoPost_ShouldPass()
	{
		var command = new CreatePostCommand
		{
			PostType = PostType.Photo,
			Content = "Sunny day",
			Photos = new List<string> { "https://cdn.example.com/p1.jpg" }
		};

		var result = _validator.Validate(command);

		result.IsValid.Should().BeTrue();
	}

	[Fact]
	public void Validate_EmptyContentAndNoPhotos_ShouldFail()
	{
		var command = new CreatePostCommand
		{
			PostType = PostType.Photo,
			Content = "   ",
			Photos = new List<string>()
		};

		var result = _validator.Validate(command);

		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(x => x.ErrorMessage.Contains("content or at least one photo", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public void Validate_RoutePostWithoutTrip_ShouldFail()
	{
		var command = new CreatePostCommand
		{
			PostType = PostType.Route,
			Content = "My route",
			TripId = null,
			Photos = new List<string>()
		};

		var result = _validator.Validate(command);

		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(x => x.PropertyName == nameof(CreatePostCommand.TripId));
	}

	[Fact]
	public void Validate_MoreThanFivePhotos_ShouldFail()
	{
		var command = new CreatePostCommand
		{
			PostType = PostType.Photo,
			Photos = Enumerable.Range(1, 6).Select(index => $"https://cdn.example.com/{index}.jpg").ToList()
		};

		var result = _validator.Validate(command);

		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(error => error.PropertyName == nameof(CreatePostCommand.Photos));
	}
}

public class CreatePostCommandHandlerTests
{
	private readonly Mock<IPostRepositoryAsync> _postRepositoryMock = new();
	private readonly Mock<IAuthenticatedUserService> _authenticatedUserServiceMock = new();
	private readonly Mock<IMapper> _mapperMock = new();

	[Fact]
	public async Task Handle_ValidCommand_ShouldSetUserAndReturnCreatedId()
	{
		var currentUserId = Guid.NewGuid();
		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(currentUserId.ToString());

		var command = new CreatePostCommand
		{
			PostType = PostType.Photo,
			Content = "Hello post",
			Photos = new List<string> { "https://cdn.example.com/p1.jpg" },
			Tags = new List<string> { "summer" }
		};

		var mappedPost = new Post { Id = Guid.NewGuid() };

		_mapperMock.Setup(x => x.Map<Post>(command)).Returns(mappedPost);
		_postRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Post>())).ReturnsAsync(mappedPost);

		var handler = new CreatePostCommandHandler(
			_postRepositoryMock.Object,
			_authenticatedUserServiceMock.Object,
			_mapperMock.Object);

		var result = await handler.Handle(command, CancellationToken.None);

		result.Should().Be(mappedPost.Id);
		mappedPost.UserId.Should().Be(currentUserId);
		_postRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Post>()), Times.Once);
	}

	[Fact]
	public async Task Handle_RoutePostWithUnavailableTrip_ShouldThrowValidationException()
	{
		var currentUserId = Guid.NewGuid();
		var tripId = Guid.NewGuid();
		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(currentUserId.ToString());
		_postRepositoryMock.Setup(x => x.CanLinkPublishedTripAsync(tripId, currentUserId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);

		var command = new CreatePostCommand
		{
			PostType = PostType.Route,
			TripId = tripId,
			Content = "Route post"
		};
		_mapperMock.Setup(x => x.Map<Post>(command)).Returns(new Post { Id = Guid.NewGuid() });

		var handler = new CreatePostCommandHandler(
			_postRepositoryMock.Object,
			_authenticatedUserServiceMock.Object,
			_mapperMock.Object);

		await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
		_postRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Post>()), Times.Never);
	}
}
