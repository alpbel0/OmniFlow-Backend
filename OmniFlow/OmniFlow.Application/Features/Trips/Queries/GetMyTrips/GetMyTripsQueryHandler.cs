using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.Trips.Queries.GetMyTrips;

public class GetMyTripsQueryHandler : IRequestHandler<GetMyTripsQuery, GetMyTripsViewModel>
{
    private readonly ITripRepositoryAsync _tripRepository;
    private readonly IApplicationDbContext _context;
    private readonly IAuthenticatedUserService _authenticatedUserService;
    private readonly IMapper _mapper;

    public GetMyTripsQueryHandler(
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
        var savedTripIds = await _context.SavedTrips
            .Where(s => s.UserId == currentUserId && tripIds.Contains(s.TripId))
            .Select(s => s.TripId)
            .ToListAsync(cancellationToken);

        foreach (var response in tripResponses)
        {
            response.IsSaved = savedTripIds.Contains(response.Id);
        }

        return new GetMyTripsViewModel(
            tripResponses.AsReadOnly(),
            request.Parameter.PageNumber,
            request.Parameter.PageSize,
            filteredTrips.Count);
    }
}