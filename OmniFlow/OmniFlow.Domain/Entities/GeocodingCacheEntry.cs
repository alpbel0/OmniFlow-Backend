using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities;

public class GeocodingCacheEntry : AuditableBaseEntity
{
    public string Provider { get; set; } = string.Empty;

    public string? ForwardKey { get; set; }

    public string? ReverseKey { get; set; }

    public string? DisplayName { get; set; }

    public string? City { get; set; }

    public string? Country { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }
}
