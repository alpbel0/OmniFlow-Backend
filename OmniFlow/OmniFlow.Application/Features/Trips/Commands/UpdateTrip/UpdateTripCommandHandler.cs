using AutoMapper;
using MediatR;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Currency;

namespace OmniFlow.Application.Features.Trips.Commands.UpdateTrip;

public class UpdateTripCommandHandler : IRequestHandler<UpdateTripCommand, Unit>
{
    private readonly ITripRepositoryAsync _tripRepository;
    private readonly IAuthenticatedUserService _authenticatedUserService;
    private readonly IMapper _mapper;
    private readonly IApplicationDbContext _context;
    private readonly ITripTemporalService _temporalService;

    public UpdateTripCommandHandler(
        ITripRepositoryAsync tripRepository,
        IAuthenticatedUserService authenticatedUserService,
        IMapper mapper,
        IApplicationDbContext context,
        ITripTemporalService temporalService)
    {
        _tripRepository = tripRepository;
        _authenticatedUserService = authenticatedUserService;
        _mapper = mapper;
        _context = context;
        _temporalService = temporalService;
    }

    public async Task<Unit> Handle(UpdateTripCommand request, CancellationToken cancellationToken)
    {
        var trip = await _tripRepository.GetByIdWithOwnerAsync(request.TripId);

        if (trip == null)
        {
            throw new EntityNotFoundException("Trip", request.TripId);
        }

        var currentUserId = Guid.Parse(_authenticatedUserService.UserId);
        if (trip.OwnerId != currentUserId)
        {
            throw new ForbiddenException("You are not authorized to update this trip.");
        }

        if (trip.Status != TripStatus.Draft)
        {
            throw new ApiException("Only draft trips can be updated.");
        }

        var normalizedCurrency = CurrencyPolicy.Normalize(request.BaseCurrencyCode);
        if (!string.Equals(trip.BaseCurrencyCode, normalizedCurrency, StringComparison.Ordinal))
        {
            trip.Destinations = await _context.TripDestinations
                .Where(x => x.TripId == trip.Id).OrderBy(x => x.OrderIndex).ToListAsync(cancellationToken);
            var execution = _temporalService.GetExecutionState(trip);
            var hasVisitLogs = await _context.PlaceVisitLogs.AnyAsync(x => x.TripId == trip.Id, cancellationToken);
            if (!execution.IsTimezoneComplete || execution.State != TripExecutionState.Upcoming || hasVisitLogs)
                throw new ApiException("Trip base currency can no longer be changed.", 409, "TRIP_BASE_CURRENCY_LOCKED");
        }

        _mapper.Map(request, trip);
        trip.BaseCurrencyCode = normalizedCurrency;
        await _tripRepository.UpdateAsync(trip);

        return Unit.Value;
    }
}
