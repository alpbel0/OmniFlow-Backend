using FluentValidation;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.Posts.Commands.CreatePost;

public class CreatePostCommandValidator : AbstractValidator<CreatePostCommand>
{
	public CreatePostCommandValidator()
	{
		RuleFor(x => x.PostType)
			.IsInEnum().WithMessage("Post type is invalid.");

		RuleFor(x => x.TripId)
			.NotNull().WithMessage("TripId is required when post type is Route.")
			.When(x => x.PostType == PostType.Route);

		RuleFor(x => x)
			.Must(HaveContentOrPhoto)
			.WithMessage("Post must contain content or at least one photo.");
	}

	private static bool HaveContentOrPhoto(CreatePostCommand command)
	{
		var hasContent = !string.IsNullOrWhiteSpace(command.Content);
		var hasPhoto = command.Photos.Count > 0;
		return hasContent || hasPhoto;
	}
}
