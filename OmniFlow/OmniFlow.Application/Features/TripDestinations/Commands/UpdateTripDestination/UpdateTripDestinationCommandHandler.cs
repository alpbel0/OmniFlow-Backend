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
    private readonly IGeocodingService _geocodingService;
    private readonly ITimeZoneResolver _timeZoneResolver;

    public UpdateTripDestinationCommandHandler(
        IApplicationDbContext context,
        IAuthenticatedUserService authenticatedUserService,
        IGeocodingService geocodingService,
        ITimeZoneResolver timeZoneResolver)
    {
        _context = context;
        _authenticatedUserService = authenticatedUserService;
        _geocodingService = geocodingService;
        _timeZoneResolver = timeZoneResolver;
    }

    public async Task<Unit> Handle(UpdateTripDestinationCommand request, CancellationToken cancellationToken)
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

            var currentUserId = Guid.Parse(_authenticatedUserService.UserId);
            if (trip.OwnerId != currentUserId)
                throw new ForbiddenException("You are not authorized to modify this trip.");

            if (trip.Status != TripStatus.Draft)
                throw new ApiException("Only draft trips can be modified.");

            var cityChanged = !string.Equals(destination.City, request.City.Trim(), StringComparison.OrdinalIgnoreCase)
                || !string.Equals(destination.Country, request.Country.Trim(), StringComparison.OrdinalIgnoreCase);
            var geocodingResult = cityChanged
                ? await _geocodingService.GeocodeAsync(request.City, request.Country, cancellationToken)
                : null;

            var oldOrderIndex = destination.OrderIndex;
            var newOrderIndex = request.OrderIndex;

            if (oldOrderIndex != newOrderIndex)
            {
                var allDestinations = await _context.TripDestinations
                    .Where(d => d.TripId == destination.TripId && d.DeletedAt == null)
                    .ToListAsync(cancellationToken);

                // Move the updated row to a temporary slot first so the unique
                // (trip_id, order_index) index does not see duplicate values
                // while the remaining destinations are being shifted.
                destination.OrderIndex = 10;
                await _context.SaveChangesAsync(cancellationToken);

                if (oldOrderIndex < newOrderIndex)
                {
                    var toShift = allDestinations
                        .Where(d => d.Id != destination.Id && d.OrderIndex > oldOrderIndex && d.OrderIndex <= newOrderIndex)
                        .ToList();

                    foreach (var dest in toShift)
                        dest.OrderIndex -= 1;
                }
                else
                {
                    var toShift = allDestinations
                        .Where(d => d.Id != destination.Id && d.OrderIndex >= newOrderIndex && d.OrderIndex < oldOrderIndex)
                        .ToList();

                    foreach (var dest in toShift)
                        dest.OrderIndex += 1;
                }

                destination.OrderIndex = newOrderIndex;
            }

            destination.UpdateDates(request.ArrivalDate, request.DepartureDate);
            destination.UpdateCity(request.City, request.Country);
            if (cityChanged)
            {
                destination.SetCoordinates(geocodingResult?.Latitude, geocodingResult?.Longitude);
                destination.Timezone = _timeZoneResolver.Resolve(geocodingResult?.Latitude, geocodingResult?.Longitude);
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
}
