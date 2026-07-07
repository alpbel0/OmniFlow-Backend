using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.DTOs.Routes;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.Trips.Queries.GetTripRoutes;

public class GetTripRoutesQueryHandler : IRequestHandler<GetTripRoutesQuery, TripRoutesResponse>
{
    private const string ProviderName = "openrouteservice";
    private const string FootWalkingProfile = "foot-walking";
    private const string FootHikingProfile = "foot-hiking";
    private const string CyclingProfile = "cycling-regular";
    private const string DrivingProfile = "driving-car";
    private const double CityModeMaxDistanceKm = 25.0;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly HashSet<PlaceCategory> NatureCategories =
    [
        PlaceCategory.Forest,
        PlaceCategory.Mountain,
        PlaceCategory.Waterfall,
        PlaceCategory.Lake,
        PlaceCategory.Cave,
        PlaceCategory.Nature
    ];

    private readonly IApplicationDbContext _context;
    private readonly IAuthenticatedUserService _authenticatedUserService;
    private readonly ITripVisibilityService _tripVisibilityService;
    private readonly IRoutingService _routingService;

    public GetTripRoutesQueryHandler(
        IApplicationDbContext context,
        IAuthenticatedUserService authenticatedUserService,
        ITripVisibilityService tripVisibilityService,
        IRoutingService routingService)
    {
        _context = context;
        _authenticatedUserService = authenticatedUserService;
        _tripVisibilityService = tripVisibilityService;
        _routingService = routingService;
    }

    public async Task<TripRoutesResponse> Handle(GetTripRoutesQuery request, CancellationToken cancellationToken)
    {
        var trip = await _context.Trips
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TripId, cancellationToken);

        if (trip is null)
            throw new EntityNotFoundException("Trip", request.TripId);

        _tripVisibilityService.EnsureVisibleOrThrow(trip, _authenticatedUserService.UserId);

        var destinations = await _context.TripDestinations
            .AsNoTracking()
            .Where(d => d.TripId == request.TripId)
            .OrderBy(d => d.OrderIndex)
            .ThenBy(d => d.Id)
            .ToListAsync(cancellationToken);

        var timelineSignals = await GetTimelineSignalsAsync(request.TripId, destinations.Select(d => d.Id), cancellationToken);
        var routeSignature = BuildRouteSignature(destinations, timelineSignals);

        var cached = await _context.TripRouteCaches
            .FirstOrDefaultAsync(c => c.TripId == request.TripId, cancellationToken);

        if (cached is not null && cached.RouteSignature == routeSignature)
        {
            var cachedResponse = JsonSerializer.Deserialize<TripRoutesResponse>(cached.ResponseJson, JsonOptions);
            if (cachedResponse is not null)
                return cachedResponse;
        }

        var response = new TripRoutesResponse
        {
            TripId = request.TripId,
            Segments = await BuildSegmentsAsync(destinations, timelineSignals, cancellationToken)
        };

        await UpsertCacheAsync(cached, request.TripId, routeSignature, response, cancellationToken);
        return response;
    }

    private async Task<List<RouteSegmentResponse>> BuildSegmentsAsync(
        List<TripDestination> destinations,
        List<TimelineRouteSignal> timelineSignals,
        CancellationToken cancellationToken)
    {
        if (destinations.Count < 2)
            return new List<RouteSegmentResponse>();

        var segmentTasks = Enumerable.Range(0, destinations.Count - 1)
            .Select(index => BuildSegmentAsync(destinations[index], destinations[index + 1], timelineSignals, cancellationToken));

        return (await Task.WhenAll(segmentTasks)).ToList();
    }

    private async Task<RouteSegmentResponse> BuildSegmentAsync(
        TripDestination from,
        TripDestination to,
        List<TimelineRouteSignal> timelineSignals,
        CancellationToken cancellationToken)
    {
        var segment = new RouteSegmentResponse
        {
            FromDestinationId = from.Id,
            ToDestinationId = to.Id
        };

        var isSameCity = string.Equals(from.City, to.City, StringComparison.OrdinalIgnoreCase);
        if (!HasCoordinates(from) || !HasCoordinates(to))
        {
            segment.Driving = RouteDetailDto.Empty();
            if (isSameCity)
            {
                segment.Walking = RouteDetailDto.Empty();
                segment.Cycling = RouteDetailDto.Empty();
            }

            return segment;
        }

        var distanceKm = CalculateDistanceKm(from.Latitude!.Value, from.Longitude!.Value, to.Latitude!.Value, to.Longitude!.Value);
        var shouldUseCityModes = isSameCity && distanceKm <= CityModeMaxDistanceKm;
        var tasks = new List<Task>();

        if (shouldUseCityModes)
        {
            var walkingProfile = UsesNatureWalking(from.Id, to.Id, timelineSignals)
                ? FootHikingProfile
                : FootWalkingProfile;
            tasks.Add(SetWalkingAsync(segment, walkingProfile, from, to, cancellationToken));
            tasks.Add(SetCyclingAsync(segment, from, to, cancellationToken));
        }

        tasks.Add(SetDrivingAsync(segment, from, to, cancellationToken));
        await Task.WhenAll(tasks);
        return segment;
    }

    private async Task SetWalkingAsync(
        RouteSegmentResponse segment,
        string profile,
        TripDestination from,
        TripDestination to,
        CancellationToken cancellationToken)
    {
        segment.Walking = await GetRouteAsync(profile, from, to, cancellationToken);
    }

    private async Task SetCyclingAsync(
        RouteSegmentResponse segment,
        TripDestination from,
        TripDestination to,
        CancellationToken cancellationToken)
    {
        segment.Cycling = await GetRouteAsync(CyclingProfile, from, to, cancellationToken);
    }

    private async Task SetDrivingAsync(
        RouteSegmentResponse segment,
        TripDestination from,
        TripDestination to,
        CancellationToken cancellationToken)
    {
        segment.Driving = await GetRouteAsync(DrivingProfile, from, to, cancellationToken);
    }

    private Task<RouteDetailDto> GetRouteAsync(
        string profile,
        TripDestination from,
        TripDestination to,
        CancellationToken cancellationToken)
    {
        return _routingService.GetRouteAsync(
            profile,
            from.Latitude!.Value,
            from.Longitude!.Value,
            to.Latitude!.Value,
            to.Longitude!.Value,
            cancellationToken);
    }

    private async Task<List<TimelineRouteSignal>> GetTimelineSignalsAsync(
        Guid tripId,
        IEnumerable<Guid> destinationIds,
        CancellationToken cancellationToken)
    {
        var destinationIdSet = destinationIds.ToList();
        if (destinationIdSet.Count == 0)
            return new List<TimelineRouteSignal>();

        return await _context.TimelineEntries
            .AsNoTracking()
            .Where(e => e.TripId == tripId && destinationIdSet.Contains(e.DestinationId))
            .Select(e => new TimelineRouteSignal(
                e.Id,
                e.DestinationId,
                e.Place != null ? e.Place.Category : null,
                e.CustomCategory))
            .ToListAsync(cancellationToken);
    }

    private static bool UsesNatureWalking(
        Guid fromDestinationId,
        Guid toDestinationId,
        List<TimelineRouteSignal> timelineSignals)
    {
        return timelineSignals.Any(signal =>
            (signal.DestinationId == fromDestinationId || signal.DestinationId == toDestinationId) &&
            ((signal.PlaceCategory.HasValue && NatureCategories.Contains(signal.PlaceCategory.Value)) ||
             (signal.CustomCategory.HasValue && NatureCategories.Contains(signal.CustomCategory.Value))));
    }

    private async Task UpsertCacheAsync(
        TripRouteCache? existing,
        Guid tripId,
        string routeSignature,
        TripRoutesResponse response,
        CancellationToken cancellationToken)
    {
        var responseJson = JsonSerializer.Serialize(response, JsonOptions);

        if (existing is null)
        {
            _context.TripRouteCaches.Add(new TripRouteCache
            {
                TripId = tripId,
                RouteSignature = routeSignature,
                ResponseJson = responseJson,
                Provider = ProviderName
            });
        }
        else
        {
            existing.RouteSignature = routeSignature;
            existing.ResponseJson = responseJson;
            existing.Provider = ProviderName;
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Cache is an optimization; route response should not fail if a concurrent request wins the write.
        }
    }

    private static string BuildRouteSignature(
        List<TripDestination> destinations,
        List<TimelineRouteSignal> timelineSignals)
    {
        var builder = new StringBuilder();

        foreach (var destination in destinations.OrderBy(d => d.OrderIndex).ThenBy(d => d.Id))
        {
            builder
                .Append("d:")
                .Append(destination.Id).Append('|')
                .Append(destination.OrderIndex).Append('|')
                .Append(destination.City.Trim().ToLowerInvariant()).Append('|')
                .Append(destination.Latitude?.ToString("F6")).Append('|')
                .Append(destination.Longitude?.ToString("F6"))
                .Append(';');
        }

        foreach (var signal in timelineSignals.OrderBy(s => s.DestinationId).ThenBy(s => s.Id))
        {
            builder
                .Append("e:")
                .Append(signal.Id).Append('|')
                .Append(signal.DestinationId).Append('|')
                .Append(signal.PlaceCategory?.ToString()).Append('|')
                .Append(signal.CustomCategory?.ToString())
                .Append(';');
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static bool HasCoordinates(TripDestination destination)
    {
        return destination.Latitude.HasValue && destination.Longitude.HasValue;
    }

    private static double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusKm = 6371.0;
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusKm * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;

    private sealed record TimelineRouteSignal(
        Guid Id,
        Guid DestinationId,
        PlaceCategory? PlaceCategory,
        PlaceCategory? CustomCategory);
}
