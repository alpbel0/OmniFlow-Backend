using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.Features.Trips.Completion;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.Trips.Queries.GetMyTrips;

public class GetMyTripsQueryHandler : IRequestHandler<GetMyTripsQuery, GetMyTripsViewModel>
{
    private readonly ITripRepositoryAsync _tripRepository;
    private readonly IApplicationDbContext _context;
    private readonly IAuthenticatedUserService _authenticatedUserService;
    private readonly IMapper _mapper;
    private readonly ITripTemporalService _temporalService;

    public GetMyTripsQueryHandler(
        ITripRepositoryAsync tripRepository,
        IApplicationDbContext context,
        IAuthenticatedUserService authenticatedUserService,
        IMapper mapper,
        ITripTemporalService temporalService)
    {
        _tripRepository = tripRepository;
        _context = context;
        _authenticatedUserService = authenticatedUserService;
        _mapper = mapper;
        _temporalService = temporalService;
    }

    public async Task<GetMyTripsViewModel> Handle(GetMyTripsQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = Guid.Parse(_authenticatedUserService.UserId);

        // Get trips by owner with pagination
        var pagedTrips = await _tripRepository.GetByOwnerAsync(currentUserId, request.Parameter);

        // Filter by status if provided
        var filteredTrips = request.Parameter.Status.HasValue
            ? pagedTrips.Data.Where(t => t.Status == request.Parameter.Status.Value).ToList()
            : pagedTrips.Data.ToList();

        var tripResponses = _mapper.Map<List<TripResponse>>(filteredTrips);

        // Set IsSaved using batch query
        var tripIds = filteredTrips.Select(t => t.Id).ToList();
        var tripById = filteredTrips.ToDictionary(t => t.Id);

        List<Guid> savedTripIds = tripIds.Count == 0
            ? new List<Guid>()
            : await _context.SavedTrips
                .Where(s => s.UserId == currentUserId && tripIds.Contains(s.TripId))
                .Select(s => s.TripId)
                .ToListAsync(cancellationToken);

        Dictionary<Guid, List<TripDestination>> destinationsByTripId = tripIds.Count == 0
            ? new Dictionary<Guid, List<TripDestination>>()
            : (await _context.TripDestinations
                .Where(d => tripIds.Contains(d.TripId) && d.DeletedAt == null)
                .OrderBy(d => d.OrderIndex)
                .ToListAsync(cancellationToken))
            .GroupBy(d => d.TripId)
            .ToDictionary(g => g.Key, g => g.ToList());

        Dictionary<Guid, List<TimelineEntry>> timelineEntriesByTripId = tripIds.Count == 0
            ? new Dictionary<Guid, List<TimelineEntry>>()
            : (await _context.TimelineEntries
                .Where(e => tripIds.Contains(e.TripId) && e.DeletedAt == null)
                .ToListAsync(cancellationToken))
            .GroupBy(e => e.TripId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var response in tripResponses)
        {
            response.IsSaved = savedTripIds.Contains(response.Id);
            var trip = tripById[response.Id];
            destinationsByTripId.TryGetValue(response.Id, out var destinations);
            timelineEntriesByTripId.TryGetValue(response.Id, out var timelineEntries);
            response.CompletionPercentage = TripCompletionCalculator.Calculate(trip, destinations, timelineEntries);
            trip.Destinations = destinations ?? [];
            var execution = _temporalService.GetExecutionState(trip);
            response.ExecutionState = execution.State;
            response.IsTimezoneComplete = execution.IsTimezoneComplete;
        }

        return new GetMyTripsViewModel(
            tripResponses.AsReadOnly(),
            request.Parameter.PageNumber,
            request.Parameter.PageSize,
            filteredTrips.Count);
    }
}
