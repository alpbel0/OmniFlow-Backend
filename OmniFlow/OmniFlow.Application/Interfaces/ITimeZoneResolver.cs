namespace OmniFlow.Application.Interfaces;

public interface ITimeZoneResolver
{
    string? Resolve(double? latitude, double? longitude);
}
