using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.TripDestinations.Commands.DeleteTripDestination;

public class DeleteTripDestinationCommandHandler : IRequestHandler<DeleteTripDestinationCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthenticatedUserService _authenticatedUserService;

    public DeleteTripDestinationCommandHandler(
        IApplicationDbContext context,
        IAuthenticatedUserService authenticatedUserService)
    {
        _context = context;
        _authenticatedUserService = authenticatedUserService;
    }

    public async Task<Unit> Handle(DeleteTripDestinationCommand request, CancellationToken cancellationToken)
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
            var deletedOrderIndex = destination.OrderIndex;

            // 2. Ownership check
            var currentUserId = Guid.Parse(_authenticatedUserService.UserId);
            if (trip.OwnerId != currentUserId)
                throw new ForbiddenException("You are not authorized to modify this trip.");

            // 3. Only draft trips can be modified
            if (trip.Status != TripStatus.Draft)
                throw new ApiException("Only draft trips can be modified.");

            // 4. Soft delete destination
            destination.DeletedAt = DateTime.UtcNow;

            // 5. Shift down subsequent destinations to close the gap
            // OrderBy: shift from lowest index to highest to avoid transient unique collisions
            // (e.g., 3->2, 4->3 — DB never sees a duplicate during the sequence)
            var toShift = trip.Destinations
                .Where(d => d.Id != destination.Id && d.OrderIndex > deletedOrderIndex && d.DeletedAt == null)
                .OrderBy(d => d.OrderIndex)
                .ToList();

            foreach (var dest in toShift)
            {
                dest.OrderIndex--;
            }

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
