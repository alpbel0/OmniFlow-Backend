using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Infrastructure.Repositories;

public class FlightRepositoryAsync : GenericRepositoryAsync<Flight>, IFlightRepositoryAsync
{
    public FlightRepositoryAsync(IApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Flight>> GetByTripAsync(Guid tripId, FlightDirection? direction = null)
    {
        var query = _dbSet
            .Where(f => f.TripId == tripId);

        if (direction.HasValue)
        {
            query = query.Where(f => f.FlightDirection == direction.Value);
        }

        return await query
            .OrderBy(f => f.FlightDirection)
            .ThenBy(f => f.DepartureAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Flight>> GetByGroupAsync(Guid itineraryGroupId)
    {
        return await _dbSet
            .Where(f => f.ItineraryGroupId == itineraryGroupId)
            .OrderBy(f => f.FlightDirection)
            .ThenBy(f => f.DepartureAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Flight>> GetBookedFlightsByDirectionAsync(Guid tripId, FlightDirection direction)
    {
        return await _dbSet
            .Where(f => f.TripId == tripId && f.FlightDirection == direction && f.IsBooked)
            .ToListAsync();
    }
}