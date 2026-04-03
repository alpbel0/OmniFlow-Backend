using AutoMapper;
using MediatR;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.Trips.Commands.UpdateTrip;

public class UpdateTripCommandHandler : IRequestHandler<UpdateTripCommand, Unit>
{
    private readonly ITripRepositoryAsync _tripRepository;
    private readonly IAuthenticatedUserService _authenticatedUserService;
    private readonly IMapper _mapper;

    public UpdateTripCommandHandler(
        ITripRepositoryAsync tripRepository,
        IAuthenticatedUserService authenticatedUserService,
        IMapper mapper)
    {
        _tripRepository = tripRepository;
        _authenticatedUserService = authenticatedUserService;
        _mapper = mapper;
    }

    public async Task<Unit> Handle(UpdateTripCommand request, CancellationToken cancellationToken)
    {
        var trip = await _tripRepository.GetByIdWithOwnerAsync(request.TripId);

        if (trip == null)
        {
            throw new EntityNotFoundException("Trip", request.TripId);
        }

        var currentUserId = Guid.Parse(_authenticatedUserService.UserId);
        if (trip.OwnerId != currentUserId)
        {
            throw new ForbiddenException("You are not authorized to update this trip.");
        }

        if (trip.Status != TripStatus.Draft)
        {
            throw new ApiException("Only draft trips can be updated.");
        }

        _mapper.Map(request, trip);
        await _tripRepository.UpdateAsync(trip);

        return Unit.Value;
    }
}