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
    private readonly IMapper _mapper;

    public GetTripByIdQueryHandler(
        ITripRepositoryAsync tripRepository,
        IApplicationDbContext context,
        IAuthenticatedUserService authenticatedUserService,
        IMapper mapper)
    {
        _tripRepository = tripRepository;
        _context = context;
        _authenticatedUserService = authenticatedUserService;
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

        var response = _mapper.Map<TripResponse>(trip);

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

        if (Guid.TryParse(_authenticatedUserService.UserId, out var currentUserId))
        {
            response.IsUpvoted = await _context.TripUpvotes.AnyAsync(
                upvote => upvote.TripId == request.TripId && upvote.UserId == currentUserId,
                cancellationToken);

            response.IsSaved = await _context.SavedTrips.AnyAsync(
                savedTrip => savedTrip.TripId == request.TripId && savedTrip.UserId == currentUserId,
                cancellationToken);
        }
        else
        {
            response.IsUpvoted = null;
            response.IsSaved = null;
        }

        return response;
    }
}