using MediatR;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.Trips.Commands.UnarchiveTrip;

public class UnarchiveTripCommandHandler : IRequestHandler<UnarchiveTripCommand, Unit>
{
    private readonly ITripRepositoryAsync _tripRepository;
    private readonly IAuthenticatedUserService _authenticatedUserService;

    public UnarchiveTripCommandHandler(
        ITripRepositoryAsync tripRepository,
        IAuthenticatedUserService authenticatedUserService)
    {
        _tripRepository = tripRepository;
        _authenticatedUserService = authenticatedUserService;
    }

    public async Task<Unit> Handle(UnarchiveTripCommand request, CancellationToken cancellationToken)
    {
        var trip = await _tripRepository.GetByIdWithOwnerAsync(request.TripId);

        if (trip == null)
        {
            throw new EntityNotFoundException("Trip", request.TripId);
        }

        var currentUserId = Guid.Parse(_authenticatedUserService.UserId);
        if (trip.OwnerId != currentUserId)
        {
            throw new ForbiddenException("You are not authorized to unarchive this trip.");
        }

        if (trip.Status != TripStatus.Archived)
        {
            throw new ApiException("Only archived trips can be unarchived.");
        }

        trip.Status = TripStatus.Published;
        await _tripRepository.UpdateAsync(trip);

        return Unit.Value;
    }
}
