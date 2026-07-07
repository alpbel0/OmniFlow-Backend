namespace OmniFlow.Infrastructure.Settings;

public class OpenRouteServiceSettings
{
    public string BaseUrl { get; set; } = "https://api.openrouteservice.org";

    public string ApiKey { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 8;
}
