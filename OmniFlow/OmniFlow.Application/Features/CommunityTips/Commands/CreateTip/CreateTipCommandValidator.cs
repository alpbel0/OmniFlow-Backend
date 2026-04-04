using FluentValidation;

namespace OmniFlow.Application.Features.CommunityTips.Commands.CreateTip;

public class CreateTipCommandValidator : AbstractValidator<CreateTipCommand>
{
	public CreateTipCommandValidator()
	{
		RuleFor(x => x.TripId).NotEmpty();
		RuleFor(x => x.Content).NotEmpty().MaximumLength(1000);
	}
}
