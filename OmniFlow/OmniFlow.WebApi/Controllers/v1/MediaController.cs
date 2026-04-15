using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.DTOs.Media;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.WebApi.Controllers.v1;

[Route("api/v1/media")]
public class MediaController : BaseApiController
{
	private const int MaxFilesPerRequest = 5;
	private const long MaxFileBytes = 5 * 1024 * 1024;
	private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
	{
		"image/jpeg",
		"image/jpg",
		"image/png",
		"image/webp"
	};

	private readonly IBlobService _blobService;

	public MediaController(IBlobService blobService)
	{
		_blobService = blobService;
	}

	[HttpPost("upload")]
	[RequestSizeLimit(MaxFilesPerRequest * MaxFileBytes)]
	[Consumes("multipart/form-data")]
	[ProducesResponseType(typeof(UploadMediaResponse), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public async Task<IActionResult> Upload(CancellationToken cancellationToken)
	{
		if (!Request.HasFormContentType)
			return BadRequest(new { message = "multipart/form-data isteği gerekli." });

		var files = Request.Form.Files.ToList();
		if (files is null || files.Count == 0)
			return BadRequest(new { message = "En az bir dosya gerekli." });

		if (files.Count > MaxFilesPerRequest)
			return BadRequest(new { message = $"En fazla {MaxFilesPerRequest} dosya yüklenebilir." });

		var validationError = ValidateFiles(files);
		if (validationError is not null)
			return BadRequest(new { message = validationError });

		var response = new UploadMediaResponse();
		foreach (var file in files)
		{
			await using var stream = file.OpenReadStream();
			var url = await _blobService.UploadAsync(
				stream,
				file.ContentType,
				file.FileName,
				"media",
				cancellationToken);

			response.Files.Add(new UploadedMediaFileResponse
			{
				FileName = file.FileName,
				Url = url
			});
		}

		return Ok(response);
	}

	private static string? ValidateFiles(IEnumerable<IFormFile> files)
	{
		foreach (var file in files)
		{
			if (file.Length == 0)
				return "Boş dosya yüklenemez.";

			if (file.Length > MaxFileBytes)
				return $"Her dosya en fazla {MaxFileBytes / (1024 * 1024)} MB olabilir.";

			if (!AllowedContentTypes.Contains(file.ContentType ?? string.Empty))
				return "Yalnızca jpeg, png veya webp resim dosyaları kabul edilir.";
		}

		return null;
	}
}
