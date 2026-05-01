using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.TripDestinations.Queries.GetTripDestinations;

public class GetTripDestinationsQueryHandler : IRequestHandler<GetTripDestinationsQuery, IReadOnlyList<TripDestinationResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthenticatedUserService _authenticatedUserService;
    private readonly IMapper _mapper;

    public GetTripDestinationsQueryHandler(
        IApplicationDbContext context,
        IAuthenticatedUserService authenticatedUserService,
        IMapper mapper)
    {
        _context = context;
        _authenticatedUserService = authenticatedUserService;
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

        if (destinations.Count == 0)
        {
            var tripExists = await _context.Trips
                .AsNoTracking()
                .AnyAsync(t => t.Id == request.TripId && t.DeletedAt == null, cancellationToken);

            if (!tripExists)
                throw new EntityNotFoundException("Trip", request.TripId);

            return new List<TripDestinationResponse>();
        }

        var trip = destinations.First().Trip!;

        // Draft trips are owner-only
        if (trip.Status == TripStatus.Draft)
        {
            var currentUserIdStr = _authenticatedUserService.UserId;
            if (string.IsNullOrEmpty(currentUserIdStr))
                throw new ForbiddenException("Authentication required to view draft trips.");

            var currentUserId = Guid.Parse(currentUserIdStr);
            if (trip.OwnerId != currentUserId)
                throw new ForbiddenException("You are not authorized to view this draft trip.");
        }

        return _mapper.Map<IReadOnlyList<TripDestinationResponse>>(destinations);
    }
}
