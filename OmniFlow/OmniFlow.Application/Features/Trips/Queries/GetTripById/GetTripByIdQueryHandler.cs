using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;

namespace OmniFlow.Application.Features.Trips.Queries.GetTripById;

public class GetTripByIdQueryHandler : IRequestHandler<GetTripByIdQuery, TripResponse>
{
    private readonly ITripRepositoryAsync _tripRepository;
    private readonly IApplicationDbContext _context;
    private readonly IAuthenticatedUserService _authenticatedUserService;
    private readonly ITripVisibilityService _tripVisibilityService;
    private readonly IMapper _mapper;

    public GetTripByIdQueryHandler(
        ITripRepositoryAsync tripRepository,
        IApplicationDbContext context,
        IAuthenticatedUserService authenticatedUserService,
        ITripVisibilityService tripVisibilityService,
        IMapper mapper)
    {
        _tripRepository = tripRepository;
        _context = context;
        _authenticatedUserService = authenticatedUserService;
        _tripVisibilityService = tripVisibilityService;
        _mapper = mapper;
    }

    public async Task<TripResponse> Handle(GetTripByIdQuery request, CancellationToken cancellationToken)
    {
        // GetByIdWithOwnerAndDestinationsAsync kullanıyoruz - Owner + Destinations include
        var trip = await _tripRepository.GetByIdWithOwnerAndDestinationsAsync(request.TripId);

        if (trip == null)
        {
            throw new EntityNotFoundException("Trip", request.TripId);
        }

        _tripVisibilityService.EnsureVisibleOrThrow(trip, _authenticatedUserService.UserId);

        Guid? currentUserId = null;

        if (Guid.TryParse(_authenticatedUserService.UserId, out var parsedUserId))
        {
            currentUserId = parsedUserId;
        }

        var response = _mapper.Map<TripResponse>(trip);

        if (ShouldIncrementViewCount(trip.OwnerId, currentUserId))
        {
            var updatedRows = await _context.IncrementTripViewCountAsync(request.TripId, cancellationToken);

            if (updatedRows > 0)
            {
                response.ViewCount += 1;
            }
        }

        // Lightweight projection: daily entry counts via GROUP BY
        var dailyCounts = await _context.TimelineEntries
            .Where(e => e.TripId == request.TripId && e.DeletedAt == null)
            .GroupBy(e => e.DayNumber)
            .Select(g => new DailyEntryCount
            {
                DayNumber = g.Key,
                EntryCount = g.Count()
            })
            .OrderBy(d => d.DayNumber)
            .ToListAsync(cancellationToken);

        if (dailyCounts.Count > 0)
        {
            response.TimelineSummary = new TimelineSummary
            {
                TotalEntryCount = dailyCounts.Sum(d => d.EntryCount),
                DailyCounts = dailyCounts
            };
        }

        if (currentUserId.HasValue)
        {
            response.IsUpvoted = await _context.TripUpvotes.AnyAsync(
                upvote => upvote.TripId == request.TripId && upvote.UserId == currentUserId.Value,
                cancellationToken);

            response.IsSaved = await _context.SavedTrips.AnyAsync(
                savedTrip => savedTrip.TripId == request.TripId && savedTrip.UserId == currentUserId.Value,
                cancellationToken);
        }
        else
        {
            response.IsUpvoted = null;
            response.IsSaved = null;
        }

        return response;
    }

    private static bool ShouldIncrementViewCount(Guid ownerId, Guid? currentUserId)
    {
        return !currentUserId.HasValue || currentUserId.Value != ownerId;
    }
}
