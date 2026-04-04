using FluentValidation;

namespace OmniFlow.Application.Features.Comments.Commands.CreateComment;

public class CreateCommentCommandValidator : AbstractValidator<CreateCommentCommand>
{
	public CreateCommentCommandValidator()
	{
		RuleFor(x => x.PostId)
			.NotEmpty();

		RuleFor(x => x.Content)
			.NotEmpty()
			.WithMessage("Content is required.");
	}
}
