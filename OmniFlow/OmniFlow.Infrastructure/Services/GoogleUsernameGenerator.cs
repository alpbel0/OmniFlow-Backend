using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using OmniFlow.Application.DTOs.Account;

namespace OmniFlow.Infrastructure.Services;

public static class GoogleUsernameGenerator
{
	public const int MinLength = 3;
	public const int MaxLength = 30;

	private static readonly Regex InvalidCharsRegex = new("[^a-z0-9_]+", RegexOptions.Compiled);
	private static readonly Regex RepeatedUnderscoresRegex = new("_+", RegexOptions.Compiled);

	public static string CreateBaseUsername(GoogleTokenPayload payload)
	{
		var source = !string.IsNullOrWhiteSpace(payload.Name)
			? payload.Name
			: payload.Email.Split('@', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

		var normalized = Normalize(source);
		return normalized.Length < MinLength ? "user" : TrimToMaxLength(normalized);
	}

	public static string WithSuffix(string baseUsername, int suffix)
	{
		if (suffix <= 0)
			return TrimToMaxLength(baseUsername);

		var suffixText = $"_{suffix}";
		var maxBaseLength = MaxLength - suffixText.Length;
		var trimmedBase = TrimToMaxLength(baseUsername, maxBaseLength).Trim('_');

		if (trimmedBase.Length < MinLength)
			trimmedBase = "user";

		return $"{trimmedBase}{suffixText}";
	}

	private static string Normalize(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return "user";

		var lower = ReplaceTurkishCharacters(value.Trim()).ToLowerInvariant();
		var decomposed = lower.Normalize(NormalizationForm.FormD);
		var builder = new StringBuilder(decomposed.Length);

		foreach (var character in decomposed)
		{
			if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
				builder.Append(character);
		}

		var ascii = builder.ToString().Normalize(NormalizationForm.FormC);
		var username = InvalidCharsRegex.Replace(ascii, "_");
		username = RepeatedUnderscoresRegex.Replace(username, "_");
		username = username.Trim('_');

		return string.IsNullOrWhiteSpace(username) ? "user" : username;
	}

	private static string TrimToMaxLength(string value, int maxLength = MaxLength)
	{
		var trimmed = value.Length <= maxLength ? value : value[..maxLength];
		trimmed = trimmed.Trim('_');
		return trimmed.Length < MinLength ? "user" : trimmed;
	}

	private static string ReplaceTurkishCharacters(string value)
	{
		return value
			.Replace('ı', 'i')
			.Replace('İ', 'I')
			.Replace('ğ', 'g')
			.Replace('Ğ', 'G')
			.Replace('ü', 'u')
			.Replace('Ü', 'U')
			.Replace('ş', 's')
			.Replace('Ş', 'S')
			.Replace('ö', 'o')
			.Replace('Ö', 'O')
			.Replace('ç', 'c')
			.Replace('Ç', 'C');
	}
}
