using OmniFlow.Application.DTOs.Account;
using OmniFlow.Infrastructure.Services;

namespace OmniFlow.UnitTests.Auth;

public class GoogleUsernameGeneratorTests
{
	[Fact]
	public void CreateBaseUsername_WithTurkishName_NormalizesToAsciiUnderscoreUsername()
	{
		var payload = new GoogleTokenPayload
		{
			Email = "fallback@example.com",
			Name = "Yiğit Özgür Şahin"
		};

		var result = GoogleUsernameGenerator.CreateBaseUsername(payload);

		result.Should().Be("yigit_ozgur_sahin");
	}

	[Fact]
	public void CreateBaseUsername_WhenNameIsBlank_UsesEmailLocalPart()
	{
		var payload = new GoogleTokenPayload
		{
			Email = "first.last+google@example.com",
			Name = "   "
		};

		var result = GoogleUsernameGenerator.CreateBaseUsername(payload);

		result.Should().Be("first_last_google");
	}

	[Theory]
	[InlineData("")]
	[InlineData("!!")]
	[InlineData("ab")]
	public void CreateBaseUsername_WhenNormalizedValueIsTooShort_UsesUserFallback(string name)
	{
		var payload = new GoogleTokenPayload
		{
			Email = "u@example.com",
			Name = name
		};

		var result = GoogleUsernameGenerator.CreateBaseUsername(payload);

		result.Should().Be("user");
	}

	[Fact]
	public void WithSuffix_TruncatesBaseSoCandidateStaysWithinMaxLength()
	{
		var baseUsername = new string('a', GoogleUsernameGenerator.MaxLength);

		var result = GoogleUsernameGenerator.WithSuffix(baseUsername, 12);

		result.Should().EndWith("_12");
		result.Length.Should().Be(GoogleUsernameGenerator.MaxLength);
	}
}
