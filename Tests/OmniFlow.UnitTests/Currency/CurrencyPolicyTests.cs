using OmniFlow.Application.Currency;

namespace OmniFlow.UnitTests.Currency;

public class CurrencyPolicyTests
{
    [Theory]
    [InlineData("try", "TRY")]
    [InlineData(" USD ", "USD")]
    [InlineData("EUR", "EUR")]
    public void Normalize_SupportedCurrency_ReturnsUppercaseCode(string input, string expected)
    {
        CurrencyPolicy.Normalize(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("GBP")]
    [InlineData("")]
    [InlineData("US")]
    public void Normalize_UnsupportedCurrency_ThrowsArgumentException(string input)
    {
        var action = () => CurrencyPolicy.Normalize(input);

        action.Should().Throw<ArgumentException>();
    }
}
