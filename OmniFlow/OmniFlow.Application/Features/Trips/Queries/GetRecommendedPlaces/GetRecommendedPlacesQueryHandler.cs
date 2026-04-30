using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.Trips.Queries.GetRecommendedPlaces;

public class GetRecommendedPlacesQueryHandler : IRequestHandler<GetRecommendedPlacesQuery, RecommendedPlacesResult>
{
    private readonly ITripRepositoryAsync _tripRepository;
    private readonly IApplicationDbContext _context;
    private readonly IRecommendationService _recommendationService;
    private readonly IAuthenticatedUserService _authService;

    public GetRecommendedPlacesQueryHandler(
        ITripRepositoryAsync tripRepository,
        IApplicationDbContext context,
        IRecommendationService recommendationService,
        IAuthenticatedUserService authService)
    {
        _tripRepository = tripRepository;
        _context = context;
        _recommendationService = recommendationService;
        _authService = authService;
    }

    public async Task<RecommendedPlacesResult> Handle(GetRecommendedPlacesQuery request, CancellationToken cancellationToken)
    {
        var trip = await _tripRepository.GetByIdWithOwnerAndDestinationsAsync(request.TripId)
            ?? throw new EntityNotFoundException("Trip", request.TripId);

        if (trip.Status != TripStatus.Published)
        {
            var currentUserId = Guid.Parse(_authService.UserId);
            if (trip.OwnerId != currentUserId)
                throw new ForbiddenException("You can only view recommended places for your own draft trips.");
        }

        var destination = trip.Destinations.FirstOrDefault(d => d.Id == request.DestinationId)
            ?? throw new ApiException("Destination not found in this trip.", 400);

        var excludedPlaceIds = await _context.TimelineEntries
            .Where(e => e.TripId == request.TripId && e.PlaceId.HasValue && e.DeletedAt == null)
            .Select(e => e.PlaceId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var budgetTier = trip.AdjustedBudgetTier ?? trip.BudgetTier;

        var result = await _recommendationService.GetRecommendedPlacesAsync(
            destination.City,
            budgetTier,
            trip.TravelCompanion,
            trip.TravelStyles.ToList(),
            trip.Tempo,
            excludedPlaceIds,
            cancellationToken);

        return result;
    }
}