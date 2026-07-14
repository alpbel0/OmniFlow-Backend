using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.Trips.Queries.SearchNearbyPlaces;

public sealed record SearchNearbyPlacesQuery(
    Guid TripId,
    Guid TripDestinationId,
    double Latitude,
    double Longitude,
    int RadiusKm,
    NearbyPlaceCategoryGroup CategoryGroup) : IRequest<IReadOnlyList<NearbyPlaceResponse>>;

public sealed class SearchNearbyPlacesQueryValidator : AbstractValidator<SearchNearbyPlacesQuery>
{
    private static readonly int[] SupportedRadii = [1, 3, 5];

    public SearchNearbyPlacesQueryValidator()
    {
        RuleFor(query => query.TripId).NotEmpty();
        RuleFor(query => query.TripDestinationId).NotEmpty();
        RuleFor(query => query.Latitude).InclusiveBetween(-90, 90);
        RuleFor(query => query.Longitude).InclusiveBetween(-180, 180);
        RuleFor(query => query.RadiusKm).Must(radius => SupportedRadii.Contains(radius))
            .WithMessage("radiusKm must be one of 1, 3 or 5.");
        RuleFor(query => query.CategoryGroup).IsInEnum();
    }
}

public sealed class SearchNearbyPlacesQueryHandler(
    IApplicationDbContext context,
    IAuthenticatedUserService authenticatedUserService,
    ITripTemporalService temporalService,
    IDateTimeService dateTimeService,
    INearbyPlaceSearchService nearbyPlaceSearchService,
    IScoringService scoringService)
    : IRequestHandler<SearchNearbyPlacesQuery, IReadOnlyList<NearbyPlaceResponse>>
{
    private const int MaximumCenterDistanceMeters = 25_000;
    private const int CandidateLimit = 100;
    private const int ResponseLimit = 20;

    public async Task<IReadOnlyList<NearbyPlaceResponse>> Handle(
        SearchNearbyPlacesQuery request,
        CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(authenticatedUserService.UserId);
        var trip = await LoadTripAsync(request.TripId, cancellationToken);
        EnsureTripCanSearch(trip, userId);
        var destination = ResolveActiveDestination(trip, request.TripDestinationId);
        EnsureSearchCenterIsNearDestination(request, destination);

        var categories = NearbyPlacePolicy.GetCategories(request.CategoryGroup);
        var candidates = await nearbyPlaceSearchService.SearchAsync(
            new NearbyPlaceSearchCriteria(
                trip.Id, request.Latitude, request.Longitude, request.RadiusKm * 1000, categories, CandidateLimit),
            cancellationToken);
        if (candidates.Count == 0)
            return [];

        return await BuildResponseAsync(trip, userId, candidates, cancellationToken);
    }

    private async Task<Trip> LoadTripAsync(Guid tripId, CancellationToken cancellationToken) =>
        await context.Trips.Include(trip => trip.Destinations)
            .FirstOrDefaultAsync(trip => trip.Id == tripId, cancellationToken)
        ?? throw new EntityNotFoundException("Trip", tripId);

    private void EnsureTripCanSearch(Trip trip, Guid userId)
    {
        if (trip.OwnerId != userId)
            throw new ForbiddenException("You are not authorized to search nearby places for this trip.");
        if (trip.Status != TripStatus.Published)
            throw new ApiException("The trip must be published.", 409, "TRIP_NOT_PUBLISHED");

        var execution = temporalService.GetExecutionState(trip);
        if (!execution.IsTimezoneComplete)
            throw new ApiException("Trip timezone information is incomplete.", 409, "TIMEZONE_UNAVAILABLE");
        if (execution.State == TripExecutionState.Upcoming)
            throw new ApiException("The trip has not started.", 409, "TRIP_NOT_STARTED");
        if (execution.State == TripExecutionState.Completed)
            throw new ApiException("The trip has completed.", 409, "TRIP_COMPLETED");
    }

    private TripDestination ResolveActiveDestination(Trip trip, Guid destinationId)
    {
        var destination = trip.Destinations.FirstOrDefault(item => item.Id == destinationId)
            ?? throw new EntityNotFoundException("TripDestination", destinationId);
        if (string.IsNullOrWhiteSpace(destination.Timezone))
            throw new ApiException("Destination timezone information is incomplete.", 409, "TIMEZONE_UNAVAILABLE");

        var localDate = temporalService.GetLocalDate(dateTimeService.NowUtc, destination.Timezone);
        if (localDate < destination.ArrivalDate || localDate > destination.DepartureDate)
            throw new ApiException("The destination is not active today.", 409, "TRIP_DESTINATION_NOT_ACTIVE");
        if (!destination.Latitude.HasValue || !destination.Longitude.HasValue)
            throw new ApiException("Destination coordinates are unavailable.", 409, "DESTINATION_COORDINATES_UNAVAILABLE");
        return destination;
    }

    private static void EnsureSearchCenterIsNearDestination(
        SearchNearbyPlacesQuery request,
        TripDestination destination)
    {
        var distance = NearbyPlacePolicy.CalculateDistanceMeters(
            request.Latitude,
            request.Longitude,
            destination.Latitude!.Value,
            destination.Longitude!.Value);
        if (distance > MaximumCenterDistanceMeters)
            throw new ApiException(
                "Search center is outside the destination travel area.",
                400,
                "SEARCH_CENTER_OUTSIDE_DESTINATION_AREA");
    }

    private async Task<IReadOnlyList<NearbyPlaceResponse>> BuildResponseAsync(
        Trip trip,
        Guid userId,
        IReadOnlyList<NearbyPlaceCandidate> candidates,
        CancellationToken cancellationToken)
    {
        var placeIds = candidates.Select(candidate => candidate.PlaceId).ToArray();
        var places = await context.Places.AsNoTracking()
            .Where(place => placeIds.Contains(place.Id) && place.IsActive)
            .ToDictionaryAsync(place => place.Id, cancellationToken);
        var visitCounts = await LoadVisitCountsAsync(userId, placeIds, cancellationToken);
        var distanceByPlaceId = candidates.ToDictionary(candidate => candidate.PlaceId, candidate => candidate.DistanceMeters);
        var effectiveBudgetTier = trip.AdjustedBudgetTier ?? trip.BudgetTier;
        var rankingCandidates = places.Values.Select(place => new NearbyPlaceRankingCandidate(
            place.Id,
            place.Name,
            CalculatePersonalizationScore(trip, place, effectiveBudgetTier),
            distanceByPlaceId[place.Id],
            visitCounts.GetValueOrDefault(place.Id))).ToArray();

        return NearbyPlacePolicy.Rank(rankingCandidates)
            .Take(ResponseLimit)
            .Select(ranked => MapResponse(places[ranked.PlaceId], ranked))
            .ToArray();
    }

    private async Task<Dictionary<Guid, int>> LoadVisitCountsAsync(
        Guid userId,
        Guid[] placeIds,
        CancellationToken cancellationToken) =>
        await context.PlaceVisitLogs.AsNoTracking()
            .Where(log => log.UserId == userId && log.PlaceId.HasValue && placeIds.Contains(log.PlaceId.Value))
            .GroupBy(log => log.PlaceId!.Value)
            .ToDictionaryAsync(group => group.Key, group => group.Count(), cancellationToken);

    private int CalculatePersonalizationScore(Trip trip, Place place, BudgetTier effectiveBudgetTier)
    {
        var score = scoringService.CalculateFinalScore(
            place.Category, trip.TravelCompanion, trip.TravelStyles, place.GoogleTags);
        return score + (place.BudgetTiers.Contains(effectiveBudgetTier) ? 20 : 0);
    }

    private static NearbyPlaceResponse MapResponse(Place place, RankedNearbyPlace ranked) => new()
    {
        Id = place.Id,
        Name = place.Name,
        Category = place.Category,
        PhotoUrl = place.PhotoUrl,
        PhotoUrls = place.PhotoUrls,
        Latitude = place.Latitude,
        Longitude = place.Longitude,
        Address = place.Address,
        City = place.City,
        Country = place.Country,
        EstimatedPrice = place.EstimatedPrice,
        CurrencyCode = place.CurrencyCode,
        PriceLevel = place.PriceLevel,
        Rating = place.Rating,
        ReviewCount = place.ReviewCount,
        DurationMinutes = place.DurationMinutes,
        IsFree = place.IsFree,
        WebsiteUrl = place.WebsiteUrl,
        Cuisine = place.Cuisine,
        BudgetTiers = place.BudgetTiers,
        GoogleTags = place.GoogleTags,
        DistanceMeters = ranked.DistanceMeters,
        IsPreviouslyVisited = ranked.PreviousVisitCount > 0,
        PreviousVisitCount = ranked.PreviousVisitCount,
        PersonalizationScore = ranked.PersonalizationScore,
        PersonalizationTier = ranked.PersonalizationTier
    };
}
