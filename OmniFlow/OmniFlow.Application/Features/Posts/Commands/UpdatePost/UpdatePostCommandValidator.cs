using FluentValidation;

namespace OmniFlow.Application.Features.Posts.Commands.UpdatePost;

public class UpdatePostCommandValidator : AbstractValidator<UpdatePostCommand>
{
    public UpdatePostCommandValidator()
    {
        RuleFor(command => command.PostId).NotEmpty();
        RuleFor(command => command.Photos)
            .Must(photos => photos == null || photos.Count <= 5)
            .WithMessage("A post can contain at most 5 photos.");
    }
}
