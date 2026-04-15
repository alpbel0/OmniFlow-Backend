using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OmniFlow.Api.IntegrationTests.Setup;
using OmniFlow.Application.DTOs.Account;
using OmniFlow.Application.DTOs.Media;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Api.IntegrationTests.Controllers;

[Collection("Integration")]
public class MediaControllerTests : IClassFixture<CustomWebApplicationFactory>
{
	private readonly CustomWebApplicationFactory _factory;
	private readonly HttpClient _client;

	private static readonly JsonSerializerOptions _json = new()
	{
		PropertyNameCaseInsensitive = true
	};

	public MediaControllerTests(CustomWebApplicationFactory factory)
	{
		_factory = factory;
		_client = CreateClientWithFakeBlobService();

		using var scope = _factory.Services.CreateScope();
		TestDatabaseSeeder.SeedAsync(scope.ServiceProvider).GetAwaiter().GetResult();
	}

	[Fact]
	public async Task Upload_WithoutToken_Returns401()
	{
		using var content = CreateMultipart(("files", "photo.jpg", "image/jpeg", "fake image"));

		var response = await _client.PostAsync("/api/v1/media/upload", content);

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task Upload_WithValidImages_ReturnsUrls()
	{
		var token = await GetAccessTokenAsync();
		using var client = CreateClientWithFakeBlobService(token);
		using var content = CreateMultipart(
			("files", "cover.jpg", "image/jpeg", "fake image"),
			("files", "stop.webp", "image/webp", "fake image"));

		var response = await client.PostAsync("/api/v1/media/upload", content);

		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var body = await response.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<UploadMediaResponse>(body, _json);

		result.Should().NotBeNull();
		result!.Files.Should().HaveCount(2);
		result.Files.Should().Contain(x => x.FileName == "cover.jpg" && x.Url == "https://blob.test/media/cover.jpg");
		result.Files.Should().Contain(x => x.FileName == "stop.webp" && x.Url == "https://blob.test/media/stop.webp");
	}

	[Fact]
	public async Task Upload_WithUnsupportedContentType_Returns400()
	{
		var token = await GetAccessTokenAsync();
		using var client = CreateClientWithFakeBlobService(token);
		using var content = CreateMultipart(("files", "notes.txt", "text/plain", "not an image"));

		var response = await client.PostAsync("/api/v1/media/upload", content);

		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	private HttpClient CreateClientWithFakeBlobService(string? token = null)
	{
		var client = _factory.WithWebHostBuilder(builder =>
		{
			builder.ConfigureServices(services =>
			{
				services.RemoveAll<IBlobService>();
				services.AddScoped<IBlobService, FakeBlobService>();
			});
		}).CreateClient();

		if (!string.IsNullOrWhiteSpace(token))
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

		return client;
	}

	private async Task<string> GetAccessTokenAsync()
	{
		var loginResponse = await _client.PostAsJsonAsync("/api/account/login", new AuthenticationRequest
		{
			Email = TestDatabaseSeeder.TestUserEmail,
			Password = TestDatabaseSeeder.TestUserPassword
		});

		loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

		var body = await loginResponse.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<AuthenticationResponse>(body, _json);
		return result!.AccessToken!;
	}

	private static MultipartFormDataContent CreateMultipart(params (string Name, string FileName, string ContentType, string Body)[] files)
	{
		var content = new MultipartFormDataContent();
		foreach (var file in files)
		{
			var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(file.Body));
			fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
			content.Add(fileContent, file.Name, file.FileName);
		}

		return content;
	}

	private sealed class FakeBlobService : IBlobService
	{
		public Task<string> UploadAsync(
			Stream stream,
			string contentType,
			string? originalFileName,
			string? folder = null,
			CancellationToken cancellationToken = default)
		{
			var safeFolder = string.IsNullOrWhiteSpace(folder) ? "root" : folder.Trim('/');
			return Task.FromResult($"https://blob.test/{safeFolder}/{originalFileName}");
		}
	}
}
