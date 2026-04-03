using MediatR;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;

namespace OmniFlow.Application.Features.Trips.Commands.DeleteTrip;

public class DeleteTripCommandHandler : IRequestHandler<DeleteTripCommand, Unit>
{
    private readonly ITripRepositoryAsync _tripRepository;
    private readonly IAuthenticatedUserService _authenticatedUserService;

    public DeleteTripCommandHandler(
        ITripRepositoryAsync tripRepository,
        IAuthenticatedUserService authenticatedUserService)
    {
        _tripRepository = tripRepository;
        _authenticatedUserService = authenticatedUserService;
    }

    public async Task<Unit> Handle(DeleteTripCommand request, CancellationToken cancellationToken)
    {
        var trip = await _tripRepository.GetByIdWithOwnerAsync(request.TripId);

        if (trip == null)
        {
            throw new EntityNotFoundException("Trip", request.TripId);
        }

        var currentUserId = Guid.Parse(_authenticatedUserService.UserId);
        if (trip.OwnerId != currentUserId)
        {
            throw new ForbiddenException("You are not authorized to delete this trip.");
        }

        await _tripRepository.DeleteAsync(trip);

        return Unit.Value;
    }
}