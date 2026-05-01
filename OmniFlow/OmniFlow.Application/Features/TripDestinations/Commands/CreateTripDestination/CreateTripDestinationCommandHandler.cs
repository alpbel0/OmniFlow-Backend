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
            var trip = await _context.Trips
                .Include(t => t.Destinations)
                .FirstOrDefaultAsync(t => t.Id == request.TripId && t.DeletedAt == null, cancellationToken);

            if (trip == null)
                throw new EntityNotFoundException("Trip", request.TripId);

            var currentUserId = Guid.Parse(_authenticatedUserService.UserId);
            if (trip.OwnerId != currentUserId)
                throw new ForbiddenException("You are not authorized to modify this trip.");

            if (trip.Status != TripStatus.Draft)
                throw new ApiException("Only draft trips can be modified.");

            var toShift = trip.Destinations
                .Where(d => d.OrderIndex >= request.OrderIndex && d.DeletedAt == null)
                .ToList();

            foreach (var dest in toShift)
                dest.OrderIndex += 1;

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

            trip.RecalculateFromDestinations();

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
