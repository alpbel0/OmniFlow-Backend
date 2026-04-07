using System.Net;
using System.Text.Json;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Wrappers;
using OmniFlow.Domain.Exceptions;

namespace OmniFlow.WebApi.Middlewares;

public class ErrorHandlerMiddleware
{
	private readonly RequestDelegate _next;
	private readonly ILogger<ErrorHandlerMiddleware> _logger;

	public ErrorHandlerMiddleware(RequestDelegate next, ILogger<ErrorHandlerMiddleware> logger)
	{
		_next = next;
		_logger = logger;
	}

	public async Task Invoke(HttpContext context)
	{
		try
		{
			await _next(context);
		}
		catch (Exception ex)
		{
			await HandleExceptionAsync(context, ex);
		}
	}

	private async Task HandleExceptionAsync(HttpContext context, Exception exception)
	{
		context.Response.ContentType = "application/json";

		ErrorResponse response;
		int statusCode;

		switch (exception)
		{
			case Application.Exceptions.ValidationException validationEx:
				statusCode = (int)HttpStatusCode.UnprocessableEntity;
				var errorDetails = validationEx.Errors.Select(e => new ValidationErrorDetail(
					e.Field,
					e.Message,
					e.Code,
					e.AttemptedValue
				)).ToList();
				response = new ErrorResponse(validationEx.Message, errorDetails);
				break;

			case ApiException apiEx:
				statusCode = apiEx.StatusCode;
				response = new ErrorResponse(apiEx.Message);
				break;

			case EntityNotFoundException notFoundEx:
				statusCode = (int)HttpStatusCode.NotFound;
				response = new ErrorResponse(notFoundEx.Message);
				break;

			case ForbiddenException forbiddenEx:
				statusCode = (int)HttpStatusCode.Forbidden;
				response = new ErrorResponse(forbiddenEx.Message);
				break;

			case DuplicateUpvoteException duplicateEx:
				statusCode = (int)HttpStatusCode.Conflict;
				response = new ErrorResponse(duplicateEx.Message);
				break;

			case SelfFollowException selfFollowEx:
				statusCode = (int)HttpStatusCode.Conflict;
				response = new ErrorResponse(selfFollowEx.Message);
				break;

			case SelfForkException selfForkEx:
				statusCode = (int)HttpStatusCode.Conflict;
				response = new ErrorResponse(selfForkEx.Message);
				break;

			case SelfUpvoteException selfUpvoteEx:
				statusCode = (int)HttpStatusCode.Conflict;
				response = new ErrorResponse(selfUpvoteEx.Message);
				break;

			default:
				_logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
				statusCode = (int)HttpStatusCode.InternalServerError;
				response = new ErrorResponse("An unexpected error occurred. Please try again later.");
				break;
		}

		context.Response.StatusCode = statusCode;

		var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		});

		await context.Response.WriteAsync(json);
	}
}
