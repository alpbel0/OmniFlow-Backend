using MediatR;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;

namespace OmniFlow.Application.Features.Stops.Commands.ReorderStops;

public class ReorderStopsCommandHandler : IRequestHandler<ReorderStopsCommand, Unit>
{
    private readonly ITripRepositoryAsync _tripRepository;
    private readonly IStopRepositoryAsync _stopRepository;
    private readonly IAuthenticatedUserService _authenticatedUserService;

    public ReorderStopsCommandHandler(
        ITripRepositoryAsync tripRepository,
        IStopRepositoryAsync stopRepository,
        IAuthenticatedUserService authenticatedUserService)
    {
        _tripRepository = tripRepository;
        _stopRepository = stopRepository;
        _authenticatedUserService = authenticatedUserService;
    }

    public async Task<Unit> Handle(ReorderStopsCommand request, CancellationToken cancellationToken)
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
            throw new ForbiddenException("You are not authorized to reorder stops in this trip.");
        }

        // Get all stops for the trip
        var stops = await _stopRepository.GetByTripAsync(request.TripId);

        foreach (var item in request.Items)
        {
            var stop = stops.FirstOrDefault(s => s.Id == item.StopId);

            if (stop == null)
            {
                throw new EntityNotFoundException("Stop", item.StopId);
            }

            // Time lock check - cannot reorder time-locked stops
            if (stop.IsTimeLocked)
            {
                throw new ApiException($"Stop {stop.Id} is time-locked and cannot be reordered.", 400);
            }

            // Calculate new OrderIndex using LexoRank pattern
            double newOrderIndex = CalculateNewOrderIndex(item, stops);

            stop.DayNumber = item.NewDayNumber;
            stop.OrderIndex = newOrderIndex;
        }

        // Update all modified stops
        var modifiedStops = stops.Where(s => request.Items.Any(i => i.StopId == s.Id)).ToList();
        foreach (var stop in modifiedStops)
        {
            await _stopRepository.UpdateAsync(stop);
        }

        return Unit.Value;
    }

    private double CalculateNewOrderIndex(ReorderStopItem item, IReadOnlyList<Domain.Entities.Stop> stops)
    {
        // If both AfterStopId and BeforeStopId are provided, calculate middle value
        if (item.AfterStopId.HasValue && item.BeforeStopId.HasValue)
        {
            var afterStop = stops.FirstOrDefault(s => s.Id == item.AfterStopId.Value);
            var beforeStop = stops.FirstOrDefault(s => s.Id == item.BeforeStopId.Value);

            if (afterStop == null || beforeStop == null)
            {
                throw new ApiException("Invalid reorder position: referenced stops not found.", 400);
            }

            // Ensure after comes before in the order
            if (afterStop.OrderIndex > beforeStop.OrderIndex)
            {
                throw new ApiException("Invalid reorder position: AfterStopId must come before BeforeStopId.", 400);
            }

            return (afterStop.OrderIndex + beforeStop.OrderIndex) / 2.0;
        }

        // If only AfterStopId is provided, insert after that stop
        if (item.AfterStopId.HasValue)
        {
            var afterStop = stops.FirstOrDefault(s => s.Id == item.AfterStopId.Value);

            if (afterStop == null)
            {
                throw new ApiException("Invalid reorder position: AfterStop not found.", 400);
            }

            return afterStop.OrderIndex + 500.0;
        }

        // If only BeforeStopId is provided, insert before that stop
        if (item.BeforeStopId.HasValue)
        {
            var beforeStop = stops.FirstOrDefault(s => s.Id == item.BeforeStopId.Value);

            if (beforeStop == null)
            {
                throw new ApiException("Invalid reorder position: BeforeStop not found.", 400);
            }

            return beforeStop.OrderIndex - 500.0;
        }

        // Neither provided - use last position in the new day
        var lastInDay = stops
            .Where(s => s.DayNumber == item.NewDayNumber && s.Id != item.StopId)
            .OrderByDescending(s => s.OrderIndex)
            .FirstOrDefault();

        return lastInDay != null ? lastInDay.OrderIndex + 1000.0 : 1000.0;
    }
}