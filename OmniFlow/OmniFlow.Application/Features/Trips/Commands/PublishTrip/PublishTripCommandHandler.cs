using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.Trips.Completion;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.Trips.Commands.PublishTrip;

public class PublishTripCommandHandler : IRequestHandler<PublishTripCommand, Unit>
{
    private readonly ITripRepositoryAsync _tripRepository;
    private readonly IApplicationDbContext _context;
    private readonly IAuthenticatedUserService _authenticatedUserService;
    private readonly IKarmaService _karmaService;

    public PublishTripCommandHandler(
        ITripRepositoryAsync tripRepository,
        IApplicationDbContext context,
        IAuthenticatedUserService authenticatedUserService,
        IKarmaService karmaService)
    {
        _tripRepository = tripRepository;
        _context = context;
        _authenticatedUserService = authenticatedUserService;
        _karmaService = karmaService;
    }

    public async Task<Unit> Handle(PublishTripCommand request, CancellationToken cancellationToken)
    {
        var trip = await _tripRepository.GetByIdWithOwnerAsync(request.TripId);

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

        var timelineEntries = await _context.TimelineEntries
            .Where(e => e.TripId == request.TripId && e.DeletedAt == null)
            .ToListAsync(cancellationToken);

        if (timelineEntries.Count() == 0)
        {
            throw new ApiException("Cannot publish a trip without any timeline entries.");
        }

        var destinations = await _context.TripDestinations
            .Where(d => d.TripId == request.TripId && d.DeletedAt == null)
            .OrderBy(d => d.OrderIndex)
            .ToListAsync(cancellationToken);

        var completionPercentage = TripCompletionCalculator.Calculate(trip, destinations, timelineEntries);
        if (completionPercentage < 80)
        {
            throw new ApiException(
                $"Trip is only {completionPercentage}% complete; publishing requires at least 80%.",
                400);
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
