using OmniFlow.Application.Features.Users.Commands.UpdateProfile;
using OmniFlow.Domain.Enums;

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
			ProfilePhotoUrl = "https://cdn.example.com/profile.jpg",
			Location = "Istanbul, Turkiye",
			UpdateLocation = true,
			TravelStyles = new List<TravelStyle> { TravelStyle.Cultural, TravelStyle.Adventure },
			UpdateTravelStyles = true
		};

		var result = _validator.Validate(command);

		result.IsValid.Should().BeTrue();
	}

	[Fact]
	public void Validate_BioLongerThan300Characters_FailsValidation()
	{
		var command = new UpdateProfileCommand
		{
			Bio = new string('a', 301),
			UpdateBio = true
		};

		var result = _validator.Validate(command);

		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(error => error.PropertyName == "Bio");
	}

	[Fact]
	public void Validate_LocationLongerThan120Characters_FailsValidation()
	{
		var command = new UpdateProfileCommand
		{
			Location = new string('a', 121),
			UpdateLocation = true
		};

		var result = _validator.Validate(command);

		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(error => error.PropertyName == "Location");
	}

	[Fact]
	public void Validate_ValidTravelStyles_PassesValidation()
	{
		var command = new UpdateProfileCommand
		{
			TravelStyles = new List<TravelStyle> { TravelStyle.Cultural, TravelStyle.Adventure },
			UpdateTravelStyles = true
		};

		var result = _validator.Validate(command);

		result.IsValid.Should().BeTrue();
	}
}
