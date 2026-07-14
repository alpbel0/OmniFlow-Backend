using GeoTimeZone;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Infrastructure.Services;

public sealed class GeoTimeZoneResolver : ITimeZoneResolver
{
    public string? Resolve(double? latitude, double? longitude)
    {
        if (!latitude.HasValue || !longitude.HasValue)
            return null;
        if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
            return null;

        try
        {
            return TimeZoneLookup.GetTimeZone(latitude.Value, longitude.Value).Result;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
