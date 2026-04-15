using System.Net;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Settings;

namespace OmniFlow.Infrastructure.Services;

public class BlobService : IBlobService
{
	private readonly BlobServiceClient _blobServiceClient;
	private readonly AzureStorageSettings _settings;
	private readonly ILogger<BlobService> _logger;

	public BlobService(
		BlobServiceClient blobServiceClient,
		IOptions<AzureStorageSettings> settings,
		ILogger<BlobService> logger)
	{
		_blobServiceClient = blobServiceClient;
		_settings = settings.Value;
		_logger = logger;
	}

	public async Task<string> UploadAsync(
		Stream stream,
		string contentType,
		string? originalFileName,
		string? folder = null,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(stream);

		try
		{
			var containerClient = _blobServiceClient.GetBlobContainerClient(_settings.ContainerName);

			await containerClient.CreateIfNotExistsAsync(
				PublicAccessType.Blob,
				cancellationToken: cancellationToken);

			var extension = ResolveExtension(originalFileName, contentType);
			var blobName = BuildBlobName(extension, folder);
			var blobClient = containerClient.GetBlobClient(blobName);

			var headers = new BlobHttpHeaders
			{
				ContentType = string.IsNullOrWhiteSpace(contentType)
					? "application/octet-stream"
					: contentType
			};

			await blobClient.UploadAsync(
				stream,
				new BlobUploadOptions { HttpHeaders = headers },
				cancellationToken);

			return blobClient.Uri.ToString();
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (RequestFailedException ex)
		{
			_logger.LogWarning(
				ex,
				"Azure Blob yükleme başarısız: Status={Status}, ErrorCode={ErrorCode}",
				ex.Status,
				ex.ErrorCode);
			throw new ApiException(
				"Dosya depolama şu an kullanılamıyor. Lütfen bir süre sonra tekrar deneyin.",
				(int)HttpStatusCode.ServiceUnavailable);
		}
		catch (IOException ex)
		{
			_logger.LogWarning(ex, "Blob yükleme sırasında akış/ağ hatası.");
			throw new ApiException(
				"Dosya okunamadı veya bağlantı kesildi. Lütfen tekrar deneyin.",
				(int)HttpStatusCode.ServiceUnavailable);
		}
		catch (HttpRequestException ex)
		{
			_logger.LogWarning(ex, "Blob yükleme sırasında HTTP/bağlantı hatası.");
			throw new ApiException(
				"Dosya depolama şu an kullanılamıyor. Lütfen bir süre sonra tekrar deneyin.",
				(int)HttpStatusCode.ServiceUnavailable);
		}
	}

	private static string ResolveExtension(string? originalFileName, string contentType)
	{
		var fromName = Path.GetExtension(originalFileName);
		if (!string.IsNullOrEmpty(fromName))
			return fromName.ToLowerInvariant();

		return ExtensionFromContentType(contentType) ?? ".bin";
	}

	private static string BuildBlobName(string extension, string? folder)
	{
		var fileName = $"{Guid.NewGuid():N}{extension}";
		if (string.IsNullOrWhiteSpace(folder))
			return fileName;

		var safeFolder = folder
			.Replace('\\', '/')
			.Trim('/')
			.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		if (safeFolder.Length == 0)
			return fileName;

		var now = DateTime.UtcNow;
		return $"{string.Join('/', safeFolder)}/{now:yyyy}/{now:MM}/{fileName}";
	}

	private static string? ExtensionFromContentType(string contentType)
	{
		if (string.IsNullOrWhiteSpace(contentType))
			return null;

		var semicolon = contentType.IndexOf(';');
		var mime = semicolon >= 0 ? contentType[..semicolon].Trim() : contentType.Trim();

		return mime.ToLowerInvariant() switch
		{
			"image/jpeg" or "image/jpg" => ".jpg",
			"image/png" => ".png",
			"image/webp" => ".webp",
			"image/gif" => ".gif",
			"image/svg+xml" => ".svg",
			_ => null
		};
	}
}
