using MediatR;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.Trips.Commands.PublishTrip;

public class PublishTripCommandHandler : IRequestHandler<PublishTripCommand, Unit>
{
    private readonly ITripRepositoryAsync _tripRepository;
    private readonly IAuthenticatedUserService _authenticatedUserService;
    private readonly IKarmaService _karmaService;

    public PublishTripCommandHandler(
        ITripRepositoryAsync tripRepository,
        IAuthenticatedUserService authenticatedUserService,
        IKarmaService karmaService)
    {
        _tripRepository = tripRepository;
        _authenticatedUserService = authenticatedUserService;
        _karmaService = karmaService;
    }

    public async Task<Unit> Handle(PublishTripCommand request, CancellationToken cancellationToken)
    {
        var trip = await _tripRepository.GetWithStopsAsync(request.TripId);

        if (trip == null)
        {
            throw new EntityNotFoundException("Trip", request.TripId);
        }

        var currentUserId = Guid.Parse(_authenticatedUserService.UserId);
        if (trip.OwnerId != currentUserId)
        {
            throw new ForbiddenException("You are not authorized to publish this trip.");
        }

        if (trip.Status != TripStatus.Draft)
        {
            throw new ApiException("Only draft trips can be published.");
        }

        if (trip.Stops.Count == 0)
        {
            throw new ApiException("Cannot publish a trip without any stops.");
        }

        trip.Status = TripStatus.Published;
        await _tripRepository.UpdateAsync(trip);
        await _karmaService.AwardKarmaAsync(
            trip.OwnerId,
            null,
            KarmaEventType.TripPublished,
            10,
            trip.Id,
            KarmaSourceType.Trip);

        return Unit.Value;
    }
}