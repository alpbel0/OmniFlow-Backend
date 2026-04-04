using AutoMapper;
using MediatR;
using OmniFlow.Application.DTOs.Flights;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.Flights.Queries.GetFlightsByTrip;

public class GetFlightsByTripQueryHandler : IRequestHandler<GetFlightsByTripQuery, FlightsByTripViewModel>
{
    private readonly ITripRepositoryAsync _tripRepository;
    private readonly IFlightRepositoryAsync _flightRepository;
    private readonly IAuthenticatedUserService _authenticatedUserService;
    private readonly IMapper _mapper;

    public GetFlightsByTripQueryHandler(
        ITripRepositoryAsync tripRepository,
        IFlightRepositoryAsync flightRepository,
        IAuthenticatedUserService authenticatedUserService,
        IMapper mapper)
    {
        _tripRepository = tripRepository;
        _flightRepository = flightRepository;
        _authenticatedUserService = authenticatedUserService;
        _mapper = mapper;
    }

    public async Task<FlightsByTripViewModel> Handle(GetFlightsByTripQuery request, CancellationToken cancellationToken)
    {
        // 1. Get trip with owner
        var trip = await _tripRepository.GetByIdWithOwnerAsync(request.TripId);

        if (trip == null)
        {
            throw new EntityNotFoundException("Trip", request.TripId);
        }

        // 2. Authorization: Published trips are public, Draft/Archived are owner-only
        var currentUserId = Guid.Parse(_authenticatedUserService.UserId);
        if (trip.Status != TripStatus.Published && trip.OwnerId != currentUserId)
        {
            throw new ForbiddenException("You can only view flights from published trips or your own trips.");
        }

        // 3. Get all flights for trip
        var flights = await _flightRepository.GetByTripAsync(request.TripId);

        // 4. Group by FlightDirection
        var viewModel = new FlightsByTripViewModel
        {
            OutboundFlights = _mapper.Map<IReadOnlyList<FlightResponse>>(
                flights.Where(f => f.FlightDirection == FlightDirection.Outbound).ToList()),
            ReturnFlights = _mapper.Map<IReadOnlyList<FlightResponse>>(
                flights.Where(f => f.FlightDirection == FlightDirection.Return).ToList())
        };

        return viewModel;
    }
}