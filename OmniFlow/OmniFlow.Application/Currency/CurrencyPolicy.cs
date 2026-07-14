namespace OmniFlow.Application.Currency;

public static class CurrencyPolicy
{
    private static readonly HashSet<string> SupportedCodes =
        new(StringComparer.Ordinal) { "TRY", "USD", "EUR" };

    public static IReadOnlyCollection<string> Supported => SupportedCodes;

    public static bool IsSupported(string? code) =>
        code is not null && SupportedCodes.Contains(code.Trim().ToUpperInvariant());

    public static string Normalize(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        var normalized = code.Trim().ToUpperInvariant();
        if (!SupportedCodes.Contains(normalized))
            throw new ArgumentException($"Unsupported currency code '{normalized}'.", nameof(code));

        return normalized;
    }
}
