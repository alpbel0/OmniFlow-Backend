using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.DTOs.Users;
using OmniFlow.Application.Features.Users.Commands.UpdateProfile;
using OmniFlow.Application.Features.Users.Queries.GetTopContributors;
using OmniFlow.Application.Features.Users.Queries.GetUserProfile;
using OmniFlow.Application.Interfaces;

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

	[HttpGet("top-contributors")]
	[AllowAnonymous]
	[ProducesResponseType(typeof(IReadOnlyList<TopContributorResponse>), StatusCodes.Status200OK)]
	public async Task<IActionResult> GetTopContributors([FromQuery] int limit = 10)
	{
		var result = await Mediator.Send(new GetTopContributorsQuery { Limit = limit });
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
			ProfilePhotoUrl = request.ProfilePhotoUrl
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
			new UpdateProfileCommand { ProfilePhotoUrl = url },
			cancellationToken);

		return Ok(new UploadProfilePhotoResponse { ProfilePhotoUrl = url });
	}
}
