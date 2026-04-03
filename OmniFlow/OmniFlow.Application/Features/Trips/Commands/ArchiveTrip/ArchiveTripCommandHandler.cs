using MediatR;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.Trips.Commands.ArchiveTrip;

public class ArchiveTripCommandHandler : IRequestHandler<ArchiveTripCommand, Unit>
{
    private readonly ITripRepositoryAsync _tripRepository;
    private readonly IAuthenticatedUserService _authenticatedUserService;

    public ArchiveTripCommandHandler(
        ITripRepositoryAsync tripRepository,
        IAuthenticatedUserService authenticatedUserService)
    {
        _tripRepository = tripRepository;
        _authenticatedUserService = authenticatedUserService;
    }

    public async Task<Unit> Handle(ArchiveTripCommand request, CancellationToken cancellationToken)
    {
        var trip = await _tripRepository.GetByIdWithOwnerAsync(request.TripId);

        if (trip == null)
        {
            throw new EntityNotFoundException("Trip", request.TripId);
        }

        var currentUserId = Guid.Parse(_authenticatedUserService.UserId);
        if (trip.OwnerId != currentUserId)
        {
            throw new ForbiddenException("You are not authorized to archive this trip.");
        }

        if (trip.Status != TripStatus.Published)
        {
            throw new ApiException("Only published trips can be archived.");
        }

        trip.Status = TripStatus.Archived;
        await _tripRepository.UpdateAsync(trip);

        return Unit.Value;
    }
}