using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.TripDestinations.Commands.ReorderDestinations;

public class ReorderDestinationsCommandHandler : IRequestHandler<ReorderDestinationsCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthenticatedUserService _authenticatedUserService;

    public ReorderDestinationsCommandHandler(
        IApplicationDbContext context,
        IAuthenticatedUserService authenticatedUserService)
    {
        _context = context;
        _authenticatedUserService = authenticatedUserService;
    }

    public async Task<Unit> Handle(ReorderDestinationsCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var trip = await _context.Trips
                .Include(t => t.Destinations.Where(d => d.DeletedAt == null))
                .Include(t => t.TimelineEntries.Where(e => e.DeletedAt == null))
                .FirstOrDefaultAsync(t => t.Id == request.TripId && t.DeletedAt == null, cancellationToken);

            if (trip == null)
                throw new EntityNotFoundException("Trip", request.TripId);

            var currentUserId = Guid.Parse(_authenticatedUserService.UserId);
            if (trip.OwnerId != currentUserId)
                throw new ForbiddenException("You are not authorized to modify this trip.");

            if (trip.Status != TripStatus.Draft)
                throw new ApiException("Only draft trips can be modified.");

            var destinations = trip.Destinations.ToList();
            EnsureExactDestinationSet(request.OrderedDestinationIds, destinations.Select(d => d.Id).ToHashSet());
            await ReorderOrderIndexesAsync(destinations, request.OrderedDestinationIds, cancellationToken);

            var oldTripStartDate = trip.StartDate;
            var destinationSnapshots = destinations.ToDictionary(
                d => d.Id,
                d => new DestinationSnapshot(
                    d.NightCount,
                    CalculateTripDay(oldTripStartDate, d.ArrivalDate)));

            var destinationsById = destinations.ToDictionary(d => d.Id);
            var timelineEntriesByDestinationId = trip.TimelineEntries
                .GroupBy(e => e.DestinationId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var currentDate = oldTripStartDate;
            for (var index = 0; index < request.OrderedDestinationIds.Count; index++)
            {
                var destinationId = request.OrderedDestinationIds[index];
                var destination = destinationsById[destinationId];
                var snapshot = destinationSnapshots[destinationId];
                var newArrivalDate = currentDate;
                var newDepartureDate = newArrivalDate.AddDays(snapshot.NightCount);
                var dayOffset = CalculateTripDay(oldTripStartDate, newArrivalDate) - snapshot.OldStartDay;

                destination.UpdateDates(newArrivalDate, newDepartureDate);

                if (dayOffset != 0 && timelineEntriesByDestinationId.TryGetValue(destinationId, out var entries))
                {
                    foreach (var entry in entries)
                        entry.UpdateDestinationAndDay(destinationId, entry.DayNumber + dayOffset);
                }

                currentDate = newDepartureDate;
            }

            trip.RecalculateFromDestinations();

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Unit.Value;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task ReorderOrderIndexesAsync(
        List<Domain.Entities.TripDestination> destinations,
        IReadOnlyList<Guid> orderedDestinationIds,
        CancellationToken cancellationToken)
    {
        var destinationsById = destinations.ToDictionary(d => d.Id);

        for (var index = 0; index < orderedDestinationIds.Count; index++)
        {
            var targetOrderIndex = index + 1;
            var destination = destinationsById[orderedDestinationIds[index]];
            var oldOrderIndex = destination.OrderIndex;

            if (oldOrderIndex == targetOrderIndex)
                continue;

            destination.OrderIndex = 0;
            await _context.SaveChangesAsync(cancellationToken);

            if (oldOrderIndex > targetOrderIndex)
                await ShiftDestinationsDownAsync(destinations, oldOrderIndex, targetOrderIndex, cancellationToken);
            else
                await ShiftDestinationsUpAsync(destinations, oldOrderIndex, targetOrderIndex, cancellationToken);

            destination.OrderIndex = targetOrderIndex;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task ShiftDestinationsDownAsync(
        IEnumerable<Domain.Entities.TripDestination> destinations,
        int oldOrderIndex,
        int targetOrderIndex,
        CancellationToken cancellationToken)
    {
        var toShift = destinations
            .Where(d => d.OrderIndex >= targetOrderIndex && d.OrderIndex < oldOrderIndex)
            .OrderByDescending(d => d.OrderIndex)
            .ToList();

        foreach (var destination in toShift)
        {
            destination.OrderIndex += 1;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task ShiftDestinationsUpAsync(
        IEnumerable<Domain.Entities.TripDestination> destinations,
        int oldOrderIndex,
        int targetOrderIndex,
        CancellationToken cancellationToken)
    {
        var toShift = destinations
            .Where(d => d.OrderIndex > oldOrderIndex && d.OrderIndex <= targetOrderIndex)
            .OrderBy(d => d.OrderIndex)
            .ToList();

        foreach (var destination in toShift)
        {
            destination.OrderIndex -= 1;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private static void EnsureExactDestinationSet(
        IReadOnlyCollection<Guid> orderedDestinationIds,
        IReadOnlySet<Guid> activeDestinationIds)
    {
        if (orderedDestinationIds.Distinct().Count() != orderedDestinationIds.Count)
            throw new ApiException("OrderedDestinationIds cannot contain duplicate destination IDs.");

        if (orderedDestinationIds.Count != activeDestinationIds.Count
            || orderedDestinationIds.Any(id => !activeDestinationIds.Contains(id)))
        {
            throw new ApiException("OrderedDestinationIds must contain every active trip destination exactly once.");
        }
    }

    private static int CalculateTripDay(DateOnly tripStartDate, DateOnly date)
    {
        return date.DayNumber - tripStartDate.DayNumber + 1;
    }

    private sealed record DestinationSnapshot(int NightCount, int OldStartDay);
}
