using AutoMapper;
using MediatR;
using OmniFlow.Application.DTOs.Providers;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;

namespace OmniFlow.Application.Features.Providers.Queries.GetProviderFlights;

public class GetProviderFlightsQueryHandler : IRequestHandler<GetProviderFlightsQuery, IReadOnlyList<ProviderFlightResponse>>
{
    private readonly IProviderFlightRepositoryAsync _flightRepository;
    private readonly ITripRepositoryAsync _tripRepository;
    private readonly IBudgetCalculationService _budgetService;
    private readonly IMapper _mapper;

    public GetProviderFlightsQueryHandler(
        IProviderFlightRepositoryAsync flightRepository,
        ITripRepositoryAsync tripRepository,
        IBudgetCalculationService budgetService,
        IMapper mapper)
    {
        _flightRepository = flightRepository;
        _tripRepository = tripRepository;
        _budgetService = budgetService;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<ProviderFlightResponse>> Handle(GetProviderFlightsQuery request, CancellationToken cancellationToken)
    {
        string fromCity;
        string toCity;
        DateOnly date;

        if (request.IsReturn)
        {
            if (!request.TripId.HasValue || request.TripId.Value == Guid.Empty)
                throw new ApiException("TripId is required for return flights.", 400);

            var trip = await _tripRepository.GetByIdWithOwnerAndDestinationsAsync(request.TripId.Value)
                ?? throw new EntityNotFoundException("Trip", request.TripId.Value);

            if (!trip.Destinations.Any())
                throw new ApiException("Trip wizard is not completed or no destinations found.", 400);

            var lastDestination = trip.Destinations.OrderByDescending(d => d.OrderIndex).First();
            fromCity = lastDestination.City;
            toCity = trip.Origin;
            date = lastDestination.DepartureDate;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.FromCity))
                throw new ApiException("FromCity is required for outbound flights.", 400);
            if (string.IsNullOrWhiteSpace(request.ToCity))
                throw new ApiException("ToCity is required for outbound flights.", 400);
            if (!request.Date.HasValue)
                throw new ApiException("Date is required for outbound flights.", 400);

            fromCity = request.FromCity;
            toCity = request.ToCity;
            date = request.Date.Value;
        }

        var personCount = request.PersonCount < 1 ? 1 : request.PersonCount;

        var flights = await _flightRepository.GetByRouteAsync(fromCity, toCity, date);

        if (!flights.Any())
            return new List<ProviderFlightResponse>();

        var seasonMultiplier = _budgetService.GetSeasonMultiplier(date);

        var responses = flights.Select(f =>
        {
            var response = _mapper.Map<ProviderFlightResponse>(f);
            response.SeasonMultiplier = seasonMultiplier;
            response.SeasonAdjustedPrice = f.Price * seasonMultiplier;
            response.TotalPrice = response.SeasonAdjustedPrice * personCount;
            return response;
        }).ToList();

        return responses;
    }
}