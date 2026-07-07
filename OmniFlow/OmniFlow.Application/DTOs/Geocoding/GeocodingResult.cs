namespace OmniFlow.Application.DTOs.Geocoding;

public class GeocodingResult
{
    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public string? DisplayName { get; set; }

    public string? City { get; set; }

    public string? Country { get; set; }
}
