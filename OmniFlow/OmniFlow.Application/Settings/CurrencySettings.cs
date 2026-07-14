namespace OmniFlow.Application.Settings;

public sealed class CurrencySettings
{
    public string BaseUrl { get; set; } = "https://api.frankfurter.dev";
    public string Provider { get; set; } = "ECB";
    public int TimeoutSeconds { get; set; } = 5;
    public bool EnableWorkers { get; set; } = true;
}
