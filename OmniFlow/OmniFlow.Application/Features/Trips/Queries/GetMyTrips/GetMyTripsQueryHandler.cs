using AutoMapper;
using MediatR;
using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.Trips.Queries.GetMyTrips;

public class GetMyTripsQueryHandler : IRequestHandler<GetMyTripsQuery, GetMyTripsViewModel>
{
    private readonly ITripRepositoryAsync _tripRepository;
    private readonly IAuthenticatedUserService _authenticatedUserService;
    private readonly IMapper _mapper;

    public GetMyTripsQueryHandler(
        ITripRepositoryAsync tripRepository,
        IAuthenticatedUserService authenticatedUserService,
        IMapper mapper)
    {
        _tripRepository = tripRepository;
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

        return new GetMyTripsViewModel(
            tripResponses.AsReadOnly(),
            request.Parameter.PageNumber,
            request.Parameter.PageSize,
            filteredTrips.Count);
    }
}