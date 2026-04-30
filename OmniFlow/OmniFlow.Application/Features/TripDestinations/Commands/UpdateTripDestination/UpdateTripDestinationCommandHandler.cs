using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.TripDestinations.Commands.UpdateTripDestination;

public class UpdateTripDestinationCommandHandler : IRequestHandler<UpdateTripDestinationCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthenticatedUserService _authenticatedUserService;

    public UpdateTripDestinationCommandHandler(
        IApplicationDbContext context,
        IAuthenticatedUserService authenticatedUserService)
    {
        _context = context;
        _authenticatedUserService = authenticatedUserService;
    }

    public async Task<Unit> Handle(UpdateTripDestinationCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Load destination with trip and all trip destinations
            var destination = await _context.TripDestinations
                .Include(d => d.Trip)
                .ThenInclude(t => t!.Destinations)
                .FirstOrDefaultAsync(d => d.Id == request.DestinationId && d.DeletedAt == null, cancellationToken);

            if (destination == null)
                throw new EntityNotFoundException("TripDestination", request.DestinationId);

            var trip = destination.Trip!;

            // 2. Ownership check
            var currentUserId = Guid.Parse(_authenticatedUserService.UserId);
            if (trip.OwnerId != currentUserId)
                throw new ForbiddenException("You are not authorized to modify this trip.");

            // 3. Only draft trips can be modified
            if (trip.Status != TripStatus.Draft)
                throw new ApiException("Only draft trips can be modified.");

            var oldOrderIndex = destination.OrderIndex;
            var newOrderIndex = request.OrderIndex;

            // 4. Directional shift if OrderIndex changed
            if (oldOrderIndex != newOrderIndex)
            {
                if (oldOrderIndex < newOrderIndex)
                {
                    // Shifting down: items between old+1 and new shift down by 1
                    // OrderBy: shift from lowest index to highest to avoid transient unique collisions
                    var toShift = trip.Destinations
                        .Where(d => d.Id != destination.Id && d.OrderIndex > oldOrderIndex && d.OrderIndex <= newOrderIndex && d.DeletedAt == null)
                        .OrderBy(d => d.OrderIndex)
                        .ToList();

                    foreach (var dest in toShift)
                    {
                        dest.OrderIndex--;
                    }
                }
                else
                {
                    // Shifting up: items between new and old-1 shift up by 1
                    // OrderByDescending: shift from highest index to lowest to avoid transient unique collisions
                    var toShift = trip.Destinations
                        .Where(d => d.Id != destination.Id && d.OrderIndex >= newOrderIndex && d.OrderIndex < oldOrderIndex && d.DeletedAt == null)
                        .OrderByDescending(d => d.OrderIndex)
                        .ToList();

                    foreach (var dest in toShift)
                    {
                        dest.OrderIndex++;
                    }
                }
            }

            // 5. Update destination
            destination.UpdateDates(request.ArrivalDate, request.DepartureDate);
            destination.UpdateCity(request.City, request.Country);
            destination.OrderIndex = newOrderIndex;

            // 6. Recalculate trip dates
            trip.RecalculateFromDestinations();

            // 7. Save and commit
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
}
