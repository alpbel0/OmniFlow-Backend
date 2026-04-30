using AutoMapper;
using MediatR;
using OmniFlow.Application.DTOs.Providers;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.Providers.Queries.GetProviderHotels;

public class GetProviderHotelsQueryHandler : IRequestHandler<GetProviderHotelsQuery, IReadOnlyList<ProviderHotelResponse>>
{
    private readonly IProviderHotelRepositoryAsync _hotelRepository;
    private readonly IBudgetCalculationService _budgetService;
    private readonly IMapper _mapper;

    public GetProviderHotelsQueryHandler(
        IProviderHotelRepositoryAsync hotelRepository,
        IBudgetCalculationService budgetService,
        IMapper mapper)
    {
        _hotelRepository = hotelRepository;
        _budgetService = budgetService;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<ProviderHotelResponse>> Handle(GetProviderHotelsQuery request, CancellationToken cancellationToken)
    {
        var nightCount = (request.CheckOut.ToDateTime(TimeOnly.MinValue) - request.CheckIn.ToDateTime(TimeOnly.MinValue)).Days;
        if (nightCount < 1)
            nightCount = 1;

        var personCount = request.PersonCount < 1 ? 1 : request.PersonCount;

        var hotels = await _hotelRepository.GetByCityAsync(request.City);

        var availableHotels = hotels.Where(h => h.IsAvailable).ToList();

        if (!availableHotels.Any())
            return new List<ProviderHotelResponse>();

        var seasonMultiplier = _budgetService.GetSeasonMultiplier(request.CheckIn);

        var (economyThreshold, standardThreshold) = _budgetService.SegmentHotel(request.City);

        var responses = availableHotels.Select(h =>
        {
            var response = _mapper.Map<ProviderHotelResponse>(h);
            response.NightCount = nightCount;
            response.SeasonMultiplier = seasonMultiplier;
            response.SeasonAdjustedPricePerNight = h.PricePerNight * seasonMultiplier;
            response.TotalPrice = response.SeasonAdjustedPricePerNight * nightCount * personCount;

            response.Segment = h.PricePerNight <= economyThreshold ? BudgetTier.Economy
                : h.PricePerNight <= standardThreshold ? BudgetTier.Standard
                : BudgetTier.Premium;

            return response;
        }).ToList();

        if (request.BudgetTier.HasValue)
        {
            responses = responses.Where(r => r.Segment == request.BudgetTier.Value).ToList();
        }

        return responses.OrderBy(r => r.SeasonAdjustedPricePerNight).ToList();
    }
}