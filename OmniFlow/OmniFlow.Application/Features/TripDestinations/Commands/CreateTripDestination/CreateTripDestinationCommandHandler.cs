using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.TripDestinations.Commands.CreateTripDestination;

public class CreateTripDestinationCommandHandler : IRequestHandler<CreateTripDestinationCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthenticatedUserService _authenticatedUserService;

    public CreateTripDestinationCommandHandler(
        IApplicationDbContext context,
        IAuthenticatedUserService authenticatedUserService)
    {
        _context = context;
        _authenticatedUserService = authenticatedUserService;
    }

    public async Task<Guid> Handle(CreateTripDestinationCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Load trip with destinations
            var trip = await _context.Trips
                .Include(t => t.Destinations)
                .FirstOrDefaultAsync(t => t.Id == request.TripId && t.DeletedAt == null, cancellationToken);

            if (trip == null)
                throw new EntityNotFoundException("Trip", request.TripId);

            // 2. Ownership check
            var currentUserId = Guid.Parse(_authenticatedUserService.UserId);
            if (trip.OwnerId != currentUserId)
                throw new ForbiddenException("You are not authorized to modify this trip.");

            // 3. Only draft trips can be modified
            if (trip.Status != TripStatus.Draft)
                throw new ApiException("Only draft trips can be modified.");

            // 4. Shift existing destinations if OrderIndex conflict
            // OrderByDescending: shift from highest index to lowest to avoid transient unique collisions
            // (e.g., 4->5, 3->4, 2->3 — DB never sees a duplicate during the sequence)
            var destinationsToShift = trip.Destinations
                .Where(d => d.OrderIndex >= request.OrderIndex && d.DeletedAt == null)
                .OrderByDescending(d => d.OrderIndex)
                .ToList();

            foreach (var dest in destinationsToShift)
            {
                dest.OrderIndex++;
            }

            // 5. Create destination
            var destination = new TripDestination(
                request.ArrivalDate,
                request.DepartureDate,
                request.City,
                request.Country,
                request.OrderIndex)
            {
                TripId = trip.Id
            };

            await _context.TripDestinations.AddAsync(destination, cancellationToken);

            // 6. Recalculate trip dates
            trip.RecalculateFromDestinations();

            // 7. Save and commit
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return destination.Id;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
