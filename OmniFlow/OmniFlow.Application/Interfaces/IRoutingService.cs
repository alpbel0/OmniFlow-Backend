using OmniFlow.Application.DTOs.Routes;

namespace OmniFlow.Application.Interfaces;

public interface IRoutingService
{
    Task<RouteDetailDto> GetRouteAsync(
        string profile,
        double fromLatitude,
        double fromLongitude,
        double toLatitude,
        double toLongitude,
        CancellationToken cancellationToken = default);
}
