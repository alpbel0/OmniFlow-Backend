using System.Text.Json;
using FluentValidation.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OmniFlow.Application.Exceptions;
using OmniFlow.Domain.Exceptions;
using OmniFlow.WebApi.Middlewares;

namespace OmniFlow.Api.IntegrationTests.Middlewares;

/// <summary>
/// Isolates ErrorHandlerMiddleware behaviour without a real database.
/// Each test spins up a minimal TestServer with a single endpoint that throws
/// the exception under test, then asserts the HTTP status code and JSON body.
/// </summary>
public class ErrorHandlerMiddlewareTests
{
    // ── helper record for parsing validation error details ────────────────────────────────
    private record ValidationErrorDetailResponse(
        string Field,
        string Message,
        string Code,
        string? AttemptedValue
    );

    // ── helpers ────────────────────────────────────────────────────────────────

    private static HttpClient BuildClient(RequestDelegate throwingEndpoint)
    {
        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.Configure(app =>
                {
                    app.UseMiddleware<ErrorHandlerMiddleware>();
                    app.Run(throwingEndpoint);
                });
                web.ConfigureServices(services =>
                    services.AddLogging(b => b.ClearProviders()));
            });

        var host = hostBuilder.Start();
        return host.GetTestClient();
    }

    private static async Task<(int StatusCode, string Message, IReadOnlyList<ValidationErrorDetailResponse>? Errors)>
        ParseResponse(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var message = root.GetProperty("message").GetString() ?? string.Empty;

        IReadOnlyList<ValidationErrorDetailResponse>? errors = null;
        if (root.TryGetProperty("errors", out var errorsEl) &&
            errorsEl.ValueKind == JsonValueKind.Array)
        {
            errors = errorsEl.EnumerateArray()
                .Select(e => new ValidationErrorDetailResponse(
                    e.GetProperty("field").GetString() ?? string.Empty,
                    e.GetProperty("message").GetString() ?? string.Empty,
                    e.GetProperty("code").GetString() ?? string.Empty,
                    e.TryGetProperty("attemptedValue", out var av) ? av.GetString() : null
                ))
                .ToList();
        }

        return ((int)response.StatusCode, message, errors);
    }

    // ── ApiException ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ApiException_WithStatusCode400_ReturnsBadRequest()
    {
        var client = BuildClient(_ => throw new ApiException("Bad request error", 400));

        var response = await client.GetAsync("/");
        var (statusCode, message, _) = await ParseResponse(response);

        statusCode.Should().Be(400);
        message.Should().Be("Bad request error");
    }

    [Fact]
    public async Task ApiException_WithStatusCode401_ReturnsUnauthorized()
    {
        var client = BuildClient(_ => throw new ApiException("Unauthorized", 401));

        var response = await client.GetAsync("/");
        var (statusCode, message, _) = await ParseResponse(response);

        statusCode.Should().Be(401);
        message.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task ApiException_WithCustomStatusCode_ReturnsCorrectCode()
    {
        var client = BuildClient(_ => throw new ApiException("Custom error", 503));

        var response = await client.GetAsync("/");
        var (statusCode, _, _) = await ParseResponse(response);

        statusCode.Should().Be(503);
    }

    // ── ValidationException → 422 ──────────────────────────────────────────────

    [Fact]
    public async Task ValidationException_Returns422_WithErrorList()
    {
        var failures = new List<ValidationFailure>
        {
            new("Email", "Email is required.") { ErrorCode = "EMAIL_REQUIRED" },
            new("Password", "Password must be at least 8 characters.") { ErrorCode = "PASSWORD_TOO_SHORT" }
        };
        var client = BuildClient(_ => throw new Application.Exceptions.ValidationException(failures));

        var response = await client.GetAsync("/");
        var (statusCode, message, errors) = await ParseResponse(response);

        statusCode.Should().Be(422);
        message.Should().Be("One or more validation failures have occurred.");
        errors.Should().NotBeNull();
        errors!.Should().HaveCount(2);
        var validationErrors = errors!;

        validationErrors[0].Field.Should().Be("Email");
        validationErrors[0].Message.Should().Be("Email is required.");
        validationErrors[0].Code.Should().Be("EMAIL_REQUIRED");

        validationErrors[1].Field.Should().Be("Password");
        validationErrors[1].Message.Should().Be("Password must be at least 8 characters.");
        validationErrors[1].Code.Should().Be("PASSWORD_TOO_SHORT");
    }

    // ── EntityNotFoundException → 404 ─────────────────────────────────────────

    [Fact]
    public async Task EntityNotFoundException_Returns404()
    {
        var id = Guid.NewGuid();
        var client = BuildClient(_ => throw new EntityNotFoundException("Trip", id));

        var response = await client.GetAsync("/");
        var (statusCode, message, _) = await ParseResponse(response);

        statusCode.Should().Be(404);
        message.Should().Contain("Trip");
        message.Should().Contain(id.ToString());
    }

    // ── ForbiddenException → 403 ──────────────────────────────────────────────

    [Fact]
    public async Task ForbiddenException_Returns403()
    {
        var client = BuildClient(_ => throw new ForbiddenException("You do not have access."));

        var response = await client.GetAsync("/");
        var (statusCode, message, _) = await ParseResponse(response);

        statusCode.Should().Be(403);
        message.Should().Be("You do not have access.");
    }

    // ── DuplicateUpvoteException → 409 ────────────────────────────────────────

    [Fact]
    public async Task DuplicateUpvoteException_Returns409()
    {
        var id = Guid.NewGuid();
        var client = BuildClient(_ => throw new DuplicateUpvoteException("Trip", id));

        var response = await client.GetAsync("/");
        var (statusCode, message, _) = await ParseResponse(response);

        statusCode.Should().Be(409);
        message.Should().Contain("Trip");
        message.Should().Contain(id.ToString());
    }

    // ── SelfFollowException → 409 ─────────────────────────────────────────────

    [Fact]
    public async Task SelfFollowException_Returns409()
    {
        var userId = Guid.NewGuid();
        var client = BuildClient(_ => throw new SelfFollowException(userId));

        var response = await client.GetAsync("/");
        var (statusCode, message, _) = await ParseResponse(response);

        statusCode.Should().Be(409);
        message.Should().Contain(userId.ToString());
    }

    // ── SelfForkException → 409 ───────────────────────────────────────────────

    [Fact]
    public async Task SelfForkException_Returns409()
    {
        var userId = Guid.NewGuid();
        var tripId = Guid.NewGuid();
        var client = BuildClient(_ => throw new SelfForkException(userId, tripId));

        var response = await client.GetAsync("/");
        var (statusCode, message, _) = await ParseResponse(response);

        statusCode.Should().Be(409);
        message.Should().Contain(userId.ToString());
        message.Should().Contain(tripId.ToString());
    }

    // ── Unhandled Exception → 500 ─────────────────────────────────────────────

    [Fact]
    public async Task UnhandledException_Returns500_WithGenericMessage()
    {
        var client = BuildClient(_ => throw new InvalidOperationException("secret internal details"));

        var response = await client.GetAsync("/");
        var (statusCode, message, _) = await ParseResponse(response);

        statusCode.Should().Be(500);
        message.Should().Be("An unexpected error occurred. Please try again later.");
        message.Should().NotContain("secret internal details");
    }

    // ── Response content-type ─────────────────────────────────────────────────

    [Fact]
    public async Task AnyException_ResponseContentType_IsApplicationJson()
    {
        var client = BuildClient(_ => throw new ApiException("test", 400));

        var response = await client.GetAsync("/");

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }
}
