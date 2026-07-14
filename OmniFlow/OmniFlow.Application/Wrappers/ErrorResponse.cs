namespace OmniFlow.Application.Wrappers;

public class ErrorResponse
{
	public string Message { get; init; }
	public IReadOnlyList<ValidationErrorDetail>? Errors { get; init; }
	public string? Code { get; init; }

	public ErrorResponse(string message, IReadOnlyList<ValidationErrorDetail>? errors = null, string? code = null)
	{
		Message = message;
		Errors = errors;
		Code = code;
	}
}

public class ValidationErrorDetail
{
	public string Field { get; init; }
	public string Message { get; init; }
	public string Code { get; init; }
	public string? AttemptedValue { get; init; }

	public ValidationErrorDetail(string field, string message, string code, string? attemptedValue = null)
	{
		Field = field;
		Message = message;
		Code = code;
		AttemptedValue = attemptedValue;
	}
}
