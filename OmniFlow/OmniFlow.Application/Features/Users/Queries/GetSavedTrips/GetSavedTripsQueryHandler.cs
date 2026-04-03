using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Wrappers;

namespace OmniFlow.Application.Features.Users.Queries.GetSavedTrips;

/// <summary>
/// Handler for getting authenticated user's saved trips with pagination.
/// Returns trips ordered by saved date descending.
/// Uses explicit join since SavedTrip doesn't have navigation properties.
/// </summary>
public class GetSavedTripsQueryHandler : IRequestHandler<GetSavedTripsQuery, PagedResponse<SavedTripResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthenticatedUserService _authenticatedUserService;
    private readonly IMapper _mapper;

    public GetSavedTripsQueryHandler(
        IApplicationDbContext context,
        IAuthenticatedUserService authenticatedUserService,
        IMapper mapper)
    {
        _context = context;
        _authenticatedUserService = authenticatedUserService;
        _mapper = mapper;
    }

    public async Task<PagedResponse<SavedTripResponse>> Handle(GetSavedTripsQuery request, CancellationToken cancellationToken)
    {
        // 1. Get current user ID
        var userId = Guid.Parse(_authenticatedUserService.UserId);

        // 2. Query saved trips - join with Trips and Users
        var savedTripsQuery = from savedTrip in _context.SavedTrips
                              join trip in _context.Trips on savedTrip.TripId equals trip.Id
                              join owner in _context.Users on trip.OwnerId equals owner.Id
                              where savedTrip.UserId == userId
                              orderby savedTrip.CreatedAt descending
                              select new
                              {
                                  SavedTrip = savedTrip,
                                  Trip = trip,
                                  Owner = owner
                              };

        // 3. Get total count
        var totalCount = await savedTripsQuery.CountAsync(cancellationToken);

        // 4. Apply pagination
        var pageNumber = request.Parameter.PageNumber;
        var pageSize = request.Parameter.PageSize;

        var savedTrips = await savedTripsQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // 5. Map to response
        var responseItems = savedTrips.Select(item =>
        {
            var tripResponse = _mapper.Map<SavedTripResponse>(item.Trip);
            tripResponse.SavedAt = item.SavedTrip.CreatedAt;
            tripResponse.TripId = item.Trip.Id;
            tripResponse.OwnerId = item.Owner.Id;
            tripResponse.OwnerUsername = item.Owner.Username;
            tripResponse.OwnerProfilePhotoUrl = item.Owner.ProfilePhotoUrl;
            return tripResponse;
        }).ToList();

        // 6. Return paged response
        return new PagedResponse<SavedTripResponse>(responseItems, pageNumber, pageSize, totalCount);
    }
}