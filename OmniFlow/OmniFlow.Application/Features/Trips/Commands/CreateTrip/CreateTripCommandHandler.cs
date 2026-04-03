using AutoMapper;
using MediatR;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.Trips.Commands.CreateTrip;

public class CreateTripCommandHandler : IRequestHandler<CreateTripCommand, Guid>
{
    private readonly ITripRepositoryAsync _tripRepository;
    private readonly IAuthenticatedUserService _authenticatedUserService;
    private readonly IMapper _mapper;

    public CreateTripCommandHandler(
        ITripRepositoryAsync tripRepository,
        IAuthenticatedUserService authenticatedUserService,
        IMapper mapper)
    {
        _tripRepository = tripRepository;
        _authenticatedUserService = authenticatedUserService;
        _mapper = mapper;
    }

    public async Task<Guid> Handle(CreateTripCommand request, CancellationToken cancellationToken)
    {
        var trip = _mapper.Map<Trip>(request);
        trip.OwnerId = Guid.Parse(_authenticatedUserService.UserId);
        trip.Status = TripStatus.Draft;

        await _tripRepository.AddAsync(trip);

        return trip.Id;
    }
}