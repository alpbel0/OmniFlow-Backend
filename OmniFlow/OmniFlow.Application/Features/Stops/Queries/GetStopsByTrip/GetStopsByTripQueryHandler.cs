using AutoMapper;
using MediatR;
using OmniFlow.Application.DTOs.Stops;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.Stops.Queries.GetStopsByTrip;

public class GetStopsByTripQueryHandler : IRequestHandler<GetStopsByTripQuery, List<StopResponse>>
{
    private readonly ITripRepositoryAsync _tripRepository;
    private readonly IStopRepositoryAsync _stopRepository;
    private readonly IAuthenticatedUserService _authenticatedUserService;
    private readonly IMapper _mapper;

    public GetStopsByTripQueryHandler(
        ITripRepositoryAsync tripRepository,
        IStopRepositoryAsync stopRepository,
        IAuthenticatedUserService authenticatedUserService,
        IMapper mapper)
    {
        _tripRepository = tripRepository;
        _stopRepository = stopRepository;
        _authenticatedUserService = authenticatedUserService;
        _mapper = mapper;
    }

    public async Task<List<StopResponse>> Handle(GetStopsByTripQuery request, CancellationToken cancellationToken)
    {
        // Get trip with owner
        var trip = await _tripRepository.GetByIdWithOwnerAsync(request.TripId);

        if (trip == null)
        {
            throw new EntityNotFoundException("Trip", request.TripId);
        }

        // Authorization: Published trips are public, Draft/Archived are owner-only
        var currentUserId = Guid.Parse(_authenticatedUserService.UserId);
        if (trip.Status != TripStatus.Published && trip.OwnerId != currentUserId)
        {
            throw new ForbiddenException("You can only view stops from published trips or your own trips.");
        }

        // Get stops with Place and FallbackPlace navigation properties
        var stops = await _stopRepository.GetByTripAsync(request.TripId);

        return _mapper.Map<List<StopResponse>>(stops);
    }
}