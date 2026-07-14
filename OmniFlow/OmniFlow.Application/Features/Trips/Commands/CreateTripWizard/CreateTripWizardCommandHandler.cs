using AutoMapper;
using MediatR;
using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace OmniFlow.Application.Features.Trips.Commands.CreateTripWizard;

public class CreateTripWizardCommandHandler : IRequestHandler<CreateTripWizardCommand, CreateTripWizardResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ITripRepositoryAsync _tripRepository;
    private readonly IAuthenticatedUserService _authenticatedUserService;
    private readonly IBudgetCalculationService _budgetService;
    private readonly IMapper _mapper;
    private readonly IGeocodingService _geocodingService;
    private readonly ITimeZoneResolver _timeZoneResolver;

    public CreateTripWizardCommandHandler(
        IApplicationDbContext context,
        ITripRepositoryAsync tripRepository,
        IAuthenticatedUserService authenticatedUserService,
        IBudgetCalculationService budgetService,
        IMapper mapper,
        IGeocodingService geocodingService,
        ITimeZoneResolver timeZoneResolver)
    {
        _context = context;
        _tripRepository = tripRepository;
        _authenticatedUserService = authenticatedUserService;
        _budgetService = budgetService;
        _mapper = mapper;
        _geocodingService = geocodingService;
        _timeZoneResolver = timeZoneResolver;
    }

    public async Task<CreateTripWizardResponse> Handle(CreateTripWizardCommand request, CancellationToken cancellationToken)
    {
        var trip = _mapper.Map<Trip>(request);
        trip.OwnerId = Guid.Parse(_authenticatedUserService.UserId);
        trip.Status = TripStatus.Draft;
        var preferredCurrency = await _context.Users
            .Where(user => user.Id == trip.OwnerId)
            .Select(user => user.PreferredCurrencyCode)
            .FirstOrDefaultAsync(cancellationToken);
        trip.BaseCurrencyCode = OmniFlow.Application.Currency.CurrencyPolicy.Normalize(
            request.BaseCurrencyCode ?? preferredCurrency ?? "USD");
        var originGeocodingResult = await _geocodingService.GeocodeAsync(
            request.Origin,
            request.OriginCountry,
            cancellationToken);
        trip.SetOriginCoordinates(originGeocodingResult?.Latitude, originGeocodingResult?.Longitude);

        foreach (var destRequest in request.Destinations.OrderBy(d => d.OrderIndex))
        {
            var geocodingResult = await _geocodingService.GeocodeAsync(
                destRequest.City,
                destRequest.Country,
                cancellationToken);

            var destination = new TripDestination(
                destRequest.ArrivalDate,
                destRequest.DepartureDate,
                destRequest.City,
                destRequest.Country,
                destRequest.OrderIndex)
            {
                TripId = trip.Id
            };
            destination.SetCoordinates(geocodingResult?.Latitude, geocodingResult?.Longitude);
            destination.Timezone = _timeZoneResolver.Resolve(geocodingResult?.Latitude, geocodingResult?.Longitude);

            trip.Destinations.Add(destination);
        }

        trip.RecalculateFromDestinations();

        var budgetResult = await _budgetService.CalculateBudgetFallbackAsync(
            request.ManualBudget,
            request.BudgetTier,
            request.Origin,
            trip.Destinations.ToList(),
            request.PersonCount);

        trip.AdjustedBudgetTier = budgetResult.AdjustedTier;
        trip.EstimatedCost = budgetResult.EstimatedCost;

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            _context.Trips.Add(trip);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        var response = _mapper.Map<CreateTripWizardResponse>(trip);
        response.BudgetMessages = budgetResult.Messages;

        return response;
    }
}
