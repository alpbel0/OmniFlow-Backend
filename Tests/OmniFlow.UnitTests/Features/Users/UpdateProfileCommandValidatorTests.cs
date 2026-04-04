using OmniFlow.Application.Features.Users.Commands.UpdateProfile;

namespace OmniFlow.UnitTests.Features.Users;

public class UpdateProfileCommandValidatorTests
{
	private readonly UpdateProfileCommandValidator _validator = new();

	[Fact]
	public void Validate_ValidCommand_PassesValidation()
	{
		var command = new UpdateProfileCommand
		{
			Bio = "Travel lover",
			ProfilePhotoUrl = "https://cdn.example.com/profile.jpg"
		};

		var result = _validator.Validate(command);

		result.IsValid.Should().BeTrue();
	}

	[Fact]
	public void Validate_BioLongerThan300Characters_FailsValidation()
	{
		var command = new UpdateProfileCommand
		{
			Bio = new string('a', 301)
		};

		var result = _validator.Validate(command);

		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(error => error.PropertyName == "Bio");
	}
}