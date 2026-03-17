namespace OmniFlow.Application.Settings;

public class OpenAISettings
{
	public string ApiKey { get; set; } = default!;
	public string Model { get; set; } = default!;
	public int MaxTokens { get; set; }
}
