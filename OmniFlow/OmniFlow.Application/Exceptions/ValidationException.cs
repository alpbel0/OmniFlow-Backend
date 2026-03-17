using FluentValidation.Results;

namespace OmniFlow.Application.Exceptions;

public class ValidationException : Exception
{
	public IReadOnlyList<string> Errors { get; }

	public ValidationException(IEnumerable<ValidationFailure> failures)
		: base("One or more validation failures have occurred.")
	{
		Errors = failures.Select(f => f.ErrorMessage).ToList();
	}
}
