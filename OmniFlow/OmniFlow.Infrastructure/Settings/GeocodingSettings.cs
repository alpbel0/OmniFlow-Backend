namespace OmniFlow.Infrastructure.Settings;

public class GeocodingSettings
{
    public string Provider { get; set; } = "Nominatim";

    public string BaseUrl { get; set; } = "https://nominatim.openstreetmap.org";

    public string UserAgent { get; set; } = "OmniFlow/1.0 (+omniflowinc@gmail.com)";

    public int TimeoutSeconds { get; set; } = 5;
}
