using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.Trips.Queries.GetBudgetSummary;

public class GetBudgetSummaryQueryHandler : IRequestHandler<GetBudgetSummaryQuery, BudgetSummaryResponse>
{
    private readonly ITripRepositoryAsync _tripRepo;
    private readonly IApplicationDbContext _context;
    private readonly IBudgetCalculationService _budgetService;
    private readonly IAuthenticatedUserService _authService;
    private readonly ITripVisibilityService _tripVisibilityService;

    public GetBudgetSummaryQueryHandler(
        ITripRepositoryAsync tripRepo,
        IApplicationDbContext context,
        IBudgetCalculationService budgetService,
        IAuthenticatedUserService authService,
        ITripVisibilityService tripVisibilityService)
    {
        _tripRepo = tripRepo;
        _context = context;
        _budgetService = budgetService;
        _authService = authService;
        _tripVisibilityService = tripVisibilityService;
    }

    public async Task<BudgetSummaryResponse> Handle(GetBudgetSummaryQuery request, CancellationToken cancellationToken)
    {
        var trip = await _tripRepo.GetByIdWithOwnerAndDestinationsAsync(request.TripId)
            ?? throw new EntityNotFoundException("Trip", request.TripId);

        _tripVisibilityService.EnsureVisibleOrThrow(trip, _authService.UserId);

        var timelineEntries = await _context.TimelineEntries
            .Where(e => e.TripId == request.TripId && e.DeletedAt == null)
            .ToListAsync(cancellationToken);

        decimal totalFlightCost = 0;
        decimal totalHotelCost = 0;
        decimal totalActivityCost = 0;
        var warnings = new List<string>();

        foreach (var entry in timelineEntries)
        {
            var destination = trip.Destinations.FirstOrDefault(d => d.Id == entry.DestinationId);
            var entryDate = GetEntryDate(entry, destination);

            switch (entry.EntryType)
            {
                case TimelineEntryType.CustomFlight:
                    if (entry.ProviderFlightId.HasValue)
                    {
                        try
                        {
                            totalFlightCost += _budgetService.CalculateFlightCost(
                                entry.ProviderFlightId.Value, trip.PersonCount, entryDate);
                        }
                        catch (EntityNotFoundException)
                        {
                            totalFlightCost += entry.Price;
                            warnings.Add($"Uçuş '{entry.CustomName ?? entry.FlightFromCity + " → " + entry.FlightToCity}' için sağlayıcı verisi bulunamadı, girilen fiyat kullanıldı.");
                        }
                    }
                    else
                    {
                        totalFlightCost += entry.Price;
                    }
                    break;

                case TimelineEntryType.CustomAccommodation:
                    if (entry.ProviderHotelId.HasValue)
                    {
                        var nightCount = GetAccommodationNightCount(entry);
                        try
                        {
                            totalHotelCost += _budgetService.CalculateHotelCost(
                                entry.ProviderHotelId.Value, trip.PersonCount, nightCount, entryDate);
                        }
                        catch (EntityNotFoundException)
                        {
                            totalHotelCost += entry.Price;
                            warnings.Add($"Otel '{entry.CustomName ?? "Konaklama"}' için sağlayıcı verisi bulunamadı, girilen fiyat kullanıldı.");
                        }
                    }
                    else
                    {
                        totalHotelCost += entry.Price;
                    }
                    break;

                case TimelineEntryType.CustomTransport:
                    totalActivityCost += entry.Price;
                    break;

                case TimelineEntryType.CustomEvent:
                    totalActivityCost += entry.Price;
                    break;

                case TimelineEntryType.Place:
                default:
                    totalActivityCost += entry.Price;
                    break;
            }
        }

        var totalCost = totalFlightCost + totalHotelCost + totalActivityCost;

        if (trip.ManualBudget.HasValue && trip.ManualBudget.Value > 0 && totalCost > trip.ManualBudget.Value)
        {
            warnings.Add($"Bütçenizi {trip.ManualBudget.Value:C0} aştınız (toplam: {totalCost:C0}).");
        }

        var seasonMultiplier = trip.Destinations.Any()
            ? _budgetService.GetSeasonMultiplier(trip.Destinations.OrderBy(d => d.OrderIndex).First().ArrivalDate)
            : 1.0m;

        return new BudgetSummaryResponse
        {
            TotalFlightCost = totalFlightCost,
            TotalHotelCost = totalHotelCost,
            TotalActivityCost = totalActivityCost,
            TotalCost = totalCost,
            ManualBudget = trip.ManualBudget,
            BudgetTier = trip.BudgetTier,
            AdjustedBudgetTier = trip.AdjustedBudgetTier,
            SeasonMultiplier = Math.Round(seasonMultiplier, 2),
            Warnings = warnings
        };
    }

    private static DateOnly GetEntryDate(Domain.Entities.TimelineEntry entry, Domain.Entities.TripDestination? destination)
    {
        if (entry.EntryType == Domain.Enums.TimelineEntryType.CustomFlight && entry.FlightDepartureAt.HasValue)
            return DateOnly.FromDateTime(entry.FlightDepartureAt.Value);

        if (entry.EntryType == Domain.Enums.TimelineEntryType.CustomAccommodation && entry.AccommodationCheckIn.HasValue)
            return DateOnly.FromDateTime(entry.AccommodationCheckIn.Value);

        if (destination != null)
            return destination.ArrivalDate.AddDays(entry.DayNumber - 1);

        return DateOnly.FromDateTime(DateTime.UtcNow);
    }

    private static int GetAccommodationNightCount(Domain.Entities.TimelineEntry entry)
    {
        if (!entry.AccommodationCheckIn.HasValue || !entry.AccommodationCheckOut.HasValue)
            return 1;

        var checkIn = DateOnly.FromDateTime(entry.AccommodationCheckIn.Value);
        var checkOut = DateOnly.FromDateTime(entry.AccommodationCheckOut.Value);
        var nightCount = checkOut.DayNumber - checkIn.DayNumber;

        return Math.Max(nightCount, 1);
    }
}
