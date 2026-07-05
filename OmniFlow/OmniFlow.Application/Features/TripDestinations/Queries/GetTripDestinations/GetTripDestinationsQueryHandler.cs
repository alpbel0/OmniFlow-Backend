using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Application.Features.TripDestinations.Queries.GetTripDestinations;

public class GetTripDestinationsQueryHandler : IRequestHandler<GetTripDestinationsQuery, IReadOnlyList<TripDestinationResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthenticatedUserService _authenticatedUserService;
    private readonly ITripVisibilityService _tripVisibilityService;
    private readonly IMapper _mapper;

    public GetTripDestinationsQueryHandler(
        IApplicationDbContext context,
        IAuthenticatedUserService authenticatedUserService,
        ITripVisibilityService tripVisibilityService,
        IMapper mapper)
    {
        _context = context;
        _authenticatedUserService = authenticatedUserService;
        _tripVisibilityService = tripVisibilityService;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<TripDestinationResponse>> Handle(GetTripDestinationsQuery request, CancellationToken cancellationToken)
    {
        var destinations = await _context.TripDestinations
            .AsNoTracking()
            .Include(d => d.Trip)
            .Where(d => d.TripId == request.TripId && d.DeletedAt == null)
            .OrderBy(d => d.OrderIndex)
            .ToListAsync(cancellationToken);

        var trip = destinations.Count > 0
            ? destinations.First().Trip!
            : await _context.Trips
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == request.TripId && t.DeletedAt == null, cancellationToken);

        if (trip is null)
            throw new EntityNotFoundException("Trip", request.TripId);

        _tripVisibilityService.EnsureVisibleOrThrow(trip, _authenticatedUserService.UserId);

        return _mapper.Map<IReadOnlyList<TripDestinationResponse>>(destinations);
    }
}
