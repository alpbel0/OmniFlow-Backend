using MediatR;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;

namespace OmniFlow.Application.Features.Stops.Commands.DeleteStop;

public class DeleteStopCommandHandler : IRequestHandler<DeleteStopCommand, Unit>
{
    private readonly ITripRepositoryAsync _tripRepository;
    private readonly IStopRepositoryAsync _stopRepository;
    private readonly IAuthenticatedUserService _authenticatedUserService;

    public DeleteStopCommandHandler(
        ITripRepositoryAsync tripRepository,
        IStopRepositoryAsync stopRepository,
        IAuthenticatedUserService authenticatedUserService)
    {
        _tripRepository = tripRepository;
        _stopRepository = stopRepository;
        _authenticatedUserService = authenticatedUserService;
    }

    public async Task<Unit> Handle(DeleteStopCommand request, CancellationToken cancellationToken)
    {
        // Get trip with owner for authorization
        var trip = await _tripRepository.GetByIdWithOwnerAsync(request.TripId);

        if (trip == null)
        {
            throw new EntityNotFoundException("Trip", request.TripId);
        }

        // Owner authorization
        var currentUserId = Guid.Parse(_authenticatedUserService.UserId);
        if (trip.OwnerId != currentUserId)
        {
            throw new ForbiddenException("You are not authorized to delete stops from this trip.");
        }

        // Get stop
        var stop = await _stopRepository.GetByIdAsync(request.StopId);

        if (stop == null)
        {
            throw new EntityNotFoundException("Stop", request.StopId);
        }

        // Verify stop belongs to the trip
        if (stop.TripId != request.TripId)
        {
            throw new ApiException("Stop does not belong to the specified trip.", 400);
        }

        // Soft delete (Stop inherits AuditableBaseEntity)
        await _stopRepository.DeleteAsync(stop);

        return Unit.Value;
    }
}