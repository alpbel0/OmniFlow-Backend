using FluentValidation.Results;

namespace OmniFlow.Application.Exceptions;

public class ValidationException : Exception
{
	public IReadOnlyList<ValidationError> Errors { get; }

	public ValidationException(IEnumerable<ValidationFailure> failures)
		: base("One or more validation failures have occurred.")
	{
		Errors = failures.Select(f => new ValidationError
		{
			Field = f.PropertyName,
			Message = f.ErrorMessage,
			Code = f.ErrorCode,
			AttemptedValue = f.AttemptedValue?.ToString()
		}).ToList();
	}
}

public class ValidationError
{
	public string Field { get; set; } = default!;
	public string Message { get; set; } = default!;
	public string Code { get; set; } = default!;
	public string? AttemptedValue { get; set; }
}
