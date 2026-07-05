using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.DTOs.Posts;
using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.DTOs.Users;
using OmniFlow.Application.Features.Posts.Queries.GetMyPosts;
using OmniFlow.Application.Features.Posts.Queries.GetPostsByUser;
using OmniFlow.Application.Features.Trips.Queries.GetPublishedTripsByUser;
using OmniFlow.Application.Features.Users.Commands.UpdateProfile;
using OmniFlow.Application.Features.Users.Queries.GetSuggestedFollows;
using OmniFlow.Application.Features.Users.Queries.GetTopContributors;
using OmniFlow.Application.Features.Users.Queries.GetUserProfile;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Wrappers;

namespace OmniFlow.WebApi.Controllers.v1;

[Route("api/v1/users")]
public class UsersController : BaseApiController
{
	private const long MaxProfilePhotoBytes = 5 * 1024 * 1024;

	private readonly IAuthenticatedUserService _authenticatedUserService;
	private readonly IBlobService _blobService;

	public UsersController(
		IAuthenticatedUserService authenticatedUserService,
		IBlobService blobService)
	{
		_authenticatedUserService = authenticatedUserService;
		_blobService = blobService;
	}

	[HttpGet("me")]
	[ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public async Task<IActionResult> Me()
	{
		var query = new GetUserProfileQuery
		{
			UserKey = _authenticatedUserService.UserId
		};

		var result = await Mediator.Send(query);
		return Ok(result);
	}

	[HttpGet("me/posts")]
	[ProducesResponseType(typeof(PagedResponse<PostResponse>), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public async Task<IActionResult> MyPosts([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
	{
		var result = await Mediator.Send(new GetMyPostsQuery
		{
			PageNumber = pageNumber,
			PageSize = pageSize
		});

		return Ok(result);
	}

	/// <summary>
	/// Returns the visible community posts for a specific user.
	/// </summary>
	[HttpGet("{userId:guid}/posts")]
	[ProducesResponseType(typeof(PagedResponse<PostResponse>), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> GetPostsByUser(
		[FromRoute] Guid userId,
		[FromQuery] int pageNumber = 1,
		[FromQuery] int pageSize = 20)
	{
		var result = await Mediator.Send(new GetPostsByUserQuery
		{
			UserId = userId,
			PageNumber = pageNumber,
			PageSize = pageSize
		});

		return Ok(result);
	}

	[HttpGet("{userId:guid}/trips")]
	[ProducesResponseType(typeof(PagedResponse<TripResponse>), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> GetTripsByUser(
		[FromRoute] Guid userId,
		[FromQuery] int pageNumber = 1,
		[FromQuery] int pageSize = 20)
	{
		var result = await Mediator.Send(new GetPublishedTripsByUserQuery
		{
			UserId = userId,
			PageNumber = pageNumber,
			PageSize = pageSize
		});

		return Ok(result);
	}

	[HttpGet("top-contributors")]
	[AllowAnonymous]
	[ProducesResponseType(typeof(IReadOnlyList<TopContributorResponse>), StatusCodes.Status200OK)]
	public async Task<IActionResult> GetTopContributors([FromQuery] int limit = 10)
	{
		var result = await Mediator.Send(new GetTopContributorsQuery { Limit = limit });
		return Ok(result);
	}

	[HttpGet("suggested-follows")]
	[Authorize]
	[ProducesResponseType(typeof(IReadOnlyList<SuggestedFollowResponse>), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public async Task<IActionResult> GetSuggestedFollows(
		[FromQuery] int limit = 6,
		[FromQuery] string? excludeUserIds = null)
	{
		var excludeIds = string.IsNullOrWhiteSpace(excludeUserIds)
			? new List<Guid>()
			: excludeUserIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
				.Select(s => Guid.TryParse(s.Trim(), out var g) ? g : Guid.Empty)
				.Where(g => g != Guid.Empty)
				.ToList();

		var result = await Mediator.Send(new GetSuggestedFollowsQuery
		{
			Limit = limit,
			ExcludeUserIds = excludeIds
		});
		return Ok(result);
	}

	[HttpGet("{username}")]
	[ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> GetByUsername([FromRoute] string username)
	{
		var query = new GetUserProfileQuery
		{
			UserKey = username
		};

		var result = await Mediator.Send(query);
		return Ok(result);
	}

	[HttpPut("me")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
	public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest request)
	{
		var command = new UpdateProfileCommand
		{
			Bio = request.Bio,
			UpdateBio = true,
			ProfilePhotoUrl = request.ProfilePhotoUrl,
			UpdateProfilePhotoUrl = true,
			Location = request.Location,
			UpdateLocation = true,
			LocationLatitude = request.LocationLatitude,
			LocationLongitude = request.LocationLongitude,
			UpdateLocationCoordinates = true,
			TravelStyles = request.TravelStyles,
			UpdateTravelStyles = true
		};

		await Mediator.Send(command);
		return NoContent();
	}

	/// <summary>Yüklenen profil fotoğrafını blob'a alır ve <c>User.ProfilePhotoUrl</c> alanını günceller.</summary>
	[HttpPost("me/profile-photo")]
	[RequestSizeLimit(MaxProfilePhotoBytes)]
	[Consumes("multipart/form-data")]
	[ProducesResponseType(typeof(UploadProfilePhotoResponse), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public async Task<IActionResult> UploadProfilePhoto(
		IFormFile file,
		CancellationToken cancellationToken)
	{
		if (file is null || file.Length == 0)
			return BadRequest(new { message = "Dosya gerekli." });

		if (file.Length > MaxProfilePhotoBytes)
			return BadRequest(new { message = $"Dosya en fazla {MaxProfilePhotoBytes / (1024 * 1024)} MB olabilir." });

		var contentType = file.ContentType ?? string.Empty;
		if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
			return BadRequest(new { message = "Yalnızca resim dosyaları kabul edilir." });

		await using var stream = file.OpenReadStream();
		var url = await _blobService.UploadAsync(
			stream,
			contentType,
			file.FileName,
			"profile-photos",
			cancellationToken);

		await Mediator.Send(
			new UpdateProfileCommand
			{
				ProfilePhotoUrl = url,
				UpdateProfilePhotoUrl = true
			},
			cancellationToken);

		return Ok(new UploadProfilePhotoResponse { ProfilePhotoUrl = url });
	}
}
