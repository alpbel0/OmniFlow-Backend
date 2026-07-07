using MediatR;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.Trips.Commands.UnpublishTrip;

public class UnpublishTripCommandHandler : IRequestHandler<UnpublishTripCommand, Unit>
{
    private readonly ITripRepositoryAsync _tripRepository;
    private readonly IAuthenticatedUserService _authenticatedUserService;

    public UnpublishTripCommandHandler(
        ITripRepositoryAsync tripRepository,
        IAuthenticatedUserService authenticatedUserService)
    {
        _tripRepository = tripRepository;
        _authenticatedUserService = authenticatedUserService;
    }

    public async Task<Unit> Handle(UnpublishTripCommand request, CancellationToken cancellationToken)
    {
        var trip = await _tripRepository.GetByIdWithOwnerAsync(request.TripId);

        if (trip == null)
        {
            throw new EntityNotFoundException("Trip", request.TripId);
        }

        var currentUserId = Guid.Parse(_authenticatedUserService.UserId);
        if (trip.OwnerId != currentUserId)
        {
            throw new ForbiddenException("You are not authorized to unpublish this trip.");
        }

        if (trip.Status != TripStatus.Published)
        {
            throw new ApiException("Only published trips can be unpublished.");
        }

        trip.Status = TripStatus.Draft;
        await _tripRepository.UpdateAsync(trip);

        return Unit.Value;
    }
}
