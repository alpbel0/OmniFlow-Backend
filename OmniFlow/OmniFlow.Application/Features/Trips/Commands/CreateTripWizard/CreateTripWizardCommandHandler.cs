using AutoMapper;
using MediatR;
using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.Trips.Commands.CreateTripWizard;

public class CreateTripWizardCommandHandler : IRequestHandler<CreateTripWizardCommand, CreateTripWizardResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ITripRepositoryAsync _tripRepository;
    private readonly IAuthenticatedUserService _authenticatedUserService;
    private readonly IBudgetCalculationService _budgetService;
    private readonly IMapper _mapper;

    public CreateTripWizardCommandHandler(
        IApplicationDbContext context,
        ITripRepositoryAsync tripRepository,
        IAuthenticatedUserService authenticatedUserService,
        IBudgetCalculationService budgetService,
        IMapper mapper)
    {
        _context = context;
        _tripRepository = tripRepository;
        _authenticatedUserService = authenticatedUserService;
        _budgetService = budgetService;
        _mapper = mapper;
    }

    public async Task<CreateTripWizardResponse> Handle(CreateTripWizardCommand request, CancellationToken cancellationToken)
    {
        var trip = _mapper.Map<Trip>(request);
        trip.OwnerId = Guid.Parse(_authenticatedUserService.UserId);
        trip.Status = TripStatus.Draft;

        foreach (var destRequest in request.Destinations.OrderBy(d => d.OrderIndex))
        {
            var destination = new TripDestination(
                destRequest.ArrivalDate,
                destRequest.DepartureDate,
                destRequest.City,
                destRequest.Country,
                destRequest.OrderIndex)
            {
                TripId = trip.Id
            };

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