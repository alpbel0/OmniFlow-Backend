using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.Trips.Checklist;
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
            var destination = await _context.TripDestinations
                .Include(d => d.Trip)
                .ThenInclude(t => t!.Destinations)
                .FirstOrDefaultAsync(d => d.Id == request.DestinationId && d.DeletedAt == null, cancellationToken);

            if (destination == null)
                throw new EntityNotFoundException("TripDestination", request.DestinationId);

            var trip = destination.Trip!;
            var deletedOrderIndex = destination.OrderIndex;

            var currentUserId = Guid.Parse(_authenticatedUserService.UserId);
            if (trip.OwnerId != currentUserId)
                throw new ForbiddenException("You are not authorized to modify this trip.");

            if (trip.Status != TripStatus.Draft)
                throw new ApiException("Only draft trips can be modified.");

            var relatedConfirmations = await _context.TripChecklistConfirmations
                .Where(c => c.TripId == trip.Id)
                .ToListAsync(cancellationToken);

            var confirmationsToRemove = relatedConfirmations
                .Where(c => TripChecklistItemKeyGenerator.BelongsToDestination(c.ItemKey, destination.Id))
                .ToList();

            if (confirmationsToRemove.Count > 0)
                _context.TripChecklistConfirmations.RemoveRange(confirmationsToRemove);

            destination.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            var toShift = trip.Destinations
                .Where(d => d.Id != destination.Id && d.OrderIndex > deletedOrderIndex && d.DeletedAt == null)
                .ToList();

            foreach (var dest in toShift)
                dest.OrderIndex -= 1;

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
}
