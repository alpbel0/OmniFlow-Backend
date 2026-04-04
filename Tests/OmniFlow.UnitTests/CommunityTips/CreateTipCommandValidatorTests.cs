using OmniFlow.Application.Features.CommunityTips.Commands.CreateTip;

namespace OmniFlow.UnitTests.CommunityTips;

public class CreateTipCommandValidatorTests
{
	private readonly CreateTipCommandValidator _validator = new();

	[Fact]
	public void Validate_ValidCommand_PassesValidation()
	{
		var command = new CreateTipCommand
		{
			TripId = Guid.NewGuid(),
			Content = "Try the local bakery at sunrise"
		};

		var result = _validator.Validate(command);

		result.IsValid.Should().BeTrue();
	}

	[Fact]
	public void Validate_EmptyContent_FailsValidation()
	{
		var command = new CreateTipCommand
		{
			TripId = Guid.NewGuid(),
			Content = "   "
		};

		var result = _validator.Validate(command);

		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateTipCommand.Content));
	}

	[Fact]
	public void Validate_EmptyTripId_FailsValidation()
	{
		var command = new CreateTipCommand
		{
			TripId = Guid.Empty,
			Content = "Useful tip"
		};

		var result = _validator.Validate(command);

		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateTipCommand.TripId));
	}
}