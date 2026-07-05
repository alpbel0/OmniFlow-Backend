using AutoMapper;
using MediatR;
using Moq;
using OmniFlow.Application.DTOs.Providers;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.Providers.Queries.GetOriginCities;
using OmniFlow.Application.Features.Providers.Queries.GetProviderFlights;
using OmniFlow.Application.Features.Providers.Queries.GetProviderHotels;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.UnitTests.Phase3;

public class ProviderQueryHandlerTests
{
    private readonly Mock<IProviderFlightRepositoryAsync> _flightRepoMock;
    private readonly Mock<IProviderHotelRepositoryAsync> _hotelRepoMock;
    private readonly Mock<ITripRepositoryAsync> _tripRepoMock;
    private readonly Mock<IBudgetCalculationService> _budgetServiceMock;
    private readonly Mock<IMapper> _mapperMock;

    public ProviderQueryHandlerTests()
    {
        _flightRepoMock = new Mock<IProviderFlightRepositoryAsync>();
        _hotelRepoMock = new Mock<IProviderHotelRepositoryAsync>();
        _tripRepoMock = new Mock<ITripRepositoryAsync>();
        _budgetServiceMock = new Mock<IBudgetCalculationService>();
        _mapperMock = new Mock<IMapper>();
    }

    #region GetOriginCities

    [Fact]
    public async Task GetOriginCities_ReturnsDistinctCities()
    {
        var flights = new List<ProviderFlight>
        {
            CreateProviderFlight("Istanbul", "Paris", 100, "IST", "CDG"),
            CreateProviderFlight("Istanbul", "London", 200, "IST", "LHR"),
            CreateProviderFlight("Paris", "Rome", 150, "CDG", "FCO"),
            CreateProviderFlight("Rome", "Berlin", 180, "FCO", "BER"),
        };

        _flightRepoMock.Setup(x => x.GetDistinctDepartureCitiesAsync())
            .ReturnsAsync(flights);

        var handler = new GetOriginCitiesQueryHandler(_flightRepoMock.Object);
        var result = await handler.Handle(new GetOriginCitiesQuery(), CancellationToken.None);

        Assert.Equal(3, result.Count);
        Assert.Equal("Istanbul", result[0].City);
        Assert.Equal("Turkey", result[0].Country);
        Assert.Equal("Paris", result[1].City);
        Assert.Equal("France", result[1].Country);
        Assert.Equal("Rome", result[2].City);
        Assert.Equal("Italy", result[2].Country);
    }

    [Fact]
    public async Task GetOriginCities_NoFlights_ReturnsEmptyList()
    {
        _flightRepoMock.Setup(x => x.GetDistinctDepartureCitiesAsync())
            .ReturnsAsync(new List<ProviderFlight>());

        var handler = new GetOriginCitiesQueryHandler(_flightRepoMock.Object);
        var result = await handler.Handle(new GetOriginCitiesQuery(), CancellationToken.None);

        Assert.Empty(result);
    }

    #endregion

    #region GetProviderFlights

[Fact]
    public async Task GetProviderFlights_Outbound_ReturnsSeasonAdjustedPrices()
    {
        var date = new DateOnly(2026, 7, 15);
        var flights = new List<ProviderFlight>
        {
            CreateProviderFlight("Istanbul", "Paris", 200, "IST", "CDG"),
            CreateProviderFlight("Istanbul", "Paris", 350, "IST", "CDG"),
        };

        _flightRepoMock.Setup(x => x.GetByRouteAsync("Istanbul", "Paris", date))
            .ReturnsAsync(flights);
        _budgetServiceMock.Setup(x => x.GetSeasonMultiplier(date))
            .Returns(1.5m);
        _mapperMock.Setup(x => x.Map<ProviderFlightResponse>(It.IsAny<ProviderFlight>()))
            .Returns((ProviderFlight f) => new ProviderFlightResponse
            {
                Id = f.Id,
                BasePrice = f.Price,
                DepartureCity = f.DepartureCity,
                ArrivalCity = f.ArrivalCity,
                SeasonAdjustedPrice = f.Price * 1.5m,
                SeasonMultiplier = 1.5m,
                TotalPrice = f.Price * 1.5m * 2,
            });

        var handler = new GetProviderFlightsQueryHandler(
            _flightRepoMock.Object, _tripRepoMock.Object, _budgetServiceMock.Object, _mapperMock.Object);

        var query = new GetProviderFlightsQuery
        {
            FromCity = "Istanbul",
            ToCity = "Paris",
            Date = date,
            PersonCount = 2,
            IsReturn = false
        };

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Equal(1.5m, r.SeasonMultiplier));
    }

    [Fact]
    public async Task GetProviderFlights_ReturnFlight_ResolvesFromTrip()
    {
        var date = new DateOnly(2026, 8, 20);
        var tripId = Guid.NewGuid();
        var trip = CreateTripWithDestinations(tripId, "Istanbul", "Paris", date);

        _tripRepoMock.Setup(x => x.GetByIdWithOwnerAndDestinationsAsync(tripId))
            .ReturnsAsync(trip);

        var returnFlights = new List<ProviderFlight> { CreateProviderFlight("Paris", "Istanbul", 250, "CDG", "IST") };
        _flightRepoMock.Setup(x => x.GetByRouteAsync("Paris", "Istanbul", date))
            .ReturnsAsync(returnFlights);
        _budgetServiceMock.Setup(x => x.GetSeasonMultiplier(date)).Returns(1.0m);
        _mapperMock.Setup(x => x.Map<ProviderFlightResponse>(It.IsAny<ProviderFlight>()))
            .Returns((ProviderFlight f) => new ProviderFlightResponse
            {
                Id = f.Id,
                BasePrice = f.Price,
                DepartureCity = f.DepartureCity,
                ArrivalCity = f.ArrivalCity,
                SeasonMultiplier = 1.0m,
                SeasonAdjustedPrice = f.Price,
                TotalPrice = f.Price,
            });

        var handler = new GetProviderFlightsQueryHandler(
            _flightRepoMock.Object, _tripRepoMock.Object, _budgetServiceMock.Object, _mapperMock.Object);

        var query = new GetProviderFlightsQuery
        {
            IsReturn = true,
            TripId = tripId,
            PersonCount = 1
        };

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Paris", result[0].DepartureCity);
        Assert.Equal("Istanbul", result[0].ArrivalCity);
    }

    [Fact]
    public async Task GetProviderFlights_ReturnFlight_InvalidTripId_ThrowsEntityNotFound()
    {
        var tripId = Guid.NewGuid();
        _tripRepoMock.Setup(x => x.GetByIdWithOwnerAndDestinationsAsync(tripId))
            .ReturnsAsync((Trip?)null);

        var handler = new GetProviderFlightsQueryHandler(
            _flightRepoMock.Object, _tripRepoMock.Object, _budgetServiceMock.Object, _mapperMock.Object);

        var query = new GetProviderFlightsQuery
        {
            IsReturn = true,
            TripId = tripId,
            PersonCount = 1
        };

        await Assert.ThrowsAsync<EntityNotFoundException>(() => handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task GetProviderFlights_ReturnFlight_NoDestinations_ThrowsApiException()
    {
        var tripId = Guid.NewGuid();
        var trip = new Trip { Id = tripId, Destinations = new List<TripDestination>() };

        _tripRepoMock.Setup(x => x.GetByIdWithOwnerAndDestinationsAsync(tripId))
            .ReturnsAsync(trip);

        var handler = new GetProviderFlightsQueryHandler(
            _flightRepoMock.Object, _tripRepoMock.Object, _budgetServiceMock.Object, _mapperMock.Object);

        var query = new GetProviderFlightsQuery
        {
            IsReturn = true,
            TripId = tripId,
            PersonCount = 1
        };

        var ex = await Assert.ThrowsAsync<ApiException>(() => handler.Handle(query, CancellationToken.None));
        Assert.Contains("no destinations", ex.Message.ToLower());
    }

    [Fact]
    public async Task GetProviderFlights_ReturnFlight_MissingTripId_ThrowsApiException()
    {
        var handler = new GetProviderFlightsQueryHandler(
            _flightRepoMock.Object, _tripRepoMock.Object, _budgetServiceMock.Object, _mapperMock.Object);

        var query = new GetProviderFlightsQuery
        {
            IsReturn = true,
            TripId = null,
            PersonCount = 1
        };

        var ex = await Assert.ThrowsAsync<ApiException>(() => handler.Handle(query, CancellationToken.None));
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task GetProviderFlights_Outbound_MissingParams_ThrowsApiException()
    {
        var handler = new GetProviderFlightsQueryHandler(
            _flightRepoMock.Object, _tripRepoMock.Object, _budgetServiceMock.Object, _mapperMock.Object);

        var query = new GetProviderFlightsQuery
        {
            IsReturn = false,
            FromCity = null,
            ToCity = "Paris",
            Date = new DateOnly(2026, 7, 15),
            PersonCount = 1
        };

        var ex = await Assert.ThrowsAsync<ApiException>(() => handler.Handle(query, CancellationToken.None));
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task GetProviderFlights_NoFlights_ReturnsEmptyList()
    {
        var date = new DateOnly(2026, 7, 15);
        _flightRepoMock.Setup(x => x.GetByRouteAsync("Istanbul", "Paris", date))
            .ReturnsAsync(new List<ProviderFlight>());

        var handler = new GetProviderFlightsQueryHandler(
            _flightRepoMock.Object, _tripRepoMock.Object, _budgetServiceMock.Object, _mapperMock.Object);

        var query = new GetProviderFlightsQuery
        {
            FromCity = "Istanbul",
            ToCity = "Paris",
            Date = date,
            PersonCount = 1,
            IsReturn = false
        };

        var result = await handler.Handle(query, CancellationToken.None);
        Assert.Empty(result);
    }

    #endregion

    #region GetProviderHotels

    [Fact]
    public async Task GetProviderHotels_ReturnsWithSegmentInfo()
    {
        var checkIn = new DateOnly(2026, 8, 10);
        var checkOut = new DateOnly(2026, 8, 13);
        var hotels = new List<ProviderHotel>
        {
            CreateProviderHotel("Paris", 50m),
            CreateProviderHotel("Paris", 120m),
            CreateProviderHotel("Paris", 300m),
        };

        _hotelRepoMock.Setup(x => x.GetByCityAndDateAsync("Paris", checkIn))
            .ReturnsAsync(hotels);
        _budgetServiceMock.Setup(x => x.GetSeasonMultiplier(checkIn))
            .Returns(1.2m);
        _budgetServiceMock.Setup(x => x.SegmentHotel("Paris"))
            .Returns((80m, 200m));
        _mapperMock.Setup(x => x.Map<ProviderHotelResponse>(It.IsAny<ProviderHotel>()))
            .Returns((ProviderHotel h) => new ProviderHotelResponse
            {
                Id = h.Id,
                HotelName = h.HotelName,
                BasePricePerNight = h.PricePerNight,
                City = h.City,
            });

        var handler = new GetProviderHotelsQueryHandler(
            _hotelRepoMock.Object, _budgetServiceMock.Object, _mapperMock.Object);

        var query = new GetProviderHotelsQuery
        {
            City = "Paris",
            CheckIn = checkIn,
            CheckOut = checkOut,
            PersonCount = 2,
            BudgetTier = null
        };

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Equal(3, result.Count);
        Assert.Contains(result, r => r.Segment == BudgetTier.Economy);
        Assert.Contains(result, r => r.Segment == BudgetTier.Standard);
        Assert.Contains(result, r => r.Segment == BudgetTier.Premium);
        Assert.All(result, r => Assert.Equal(3, r.NightCount));
        Assert.Equal(1.2m, result[0].SeasonMultiplier);
    }

    [Fact]
    public async Task GetProviderHotels_BudgetTierFilter_ReturnsOnlyMatchingSegment()
    {
        var checkIn = new DateOnly(2026, 8, 10);
        var checkOut = new DateOnly(2026, 8, 13);
        var hotels = new List<ProviderHotel>
        {
            CreateProviderHotel("Paris", 50m),
            CreateProviderHotel("Paris", 120m),
            CreateProviderHotel("Paris", 300m),
        };

        _hotelRepoMock.Setup(x => x.GetByCityAndDateAsync("Paris", checkIn))
            .ReturnsAsync(hotels);
        _budgetServiceMock.Setup(x => x.GetSeasonMultiplier(checkIn)).Returns(1.0m);
        _budgetServiceMock.Setup(x => x.SegmentHotel("Paris")).Returns((80m, 200m));
        _mapperMock.Setup(x => x.Map<ProviderHotelResponse>(It.IsAny<ProviderHotel>()))
            .Returns((ProviderHotel h) => new ProviderHotelResponse
            {
                Id = h.Id,
                BasePricePerNight = h.PricePerNight,
            });

        var handler = new GetProviderHotelsQueryHandler(
            _hotelRepoMock.Object, _budgetServiceMock.Object, _mapperMock.Object);

        var query = new GetProviderHotelsQuery
        {
            City = "Paris",
            CheckIn = checkIn,
            CheckOut = checkOut,
            PersonCount = 1,
            BudgetTier = BudgetTier.Standard
        };

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(BudgetTier.Standard, result[0].Segment);
    }

    [Fact]
    public async Task GetProviderHotels_NoHotels_ReturnsEmptyList()
    {
        var checkIn = new DateOnly(2026, 8, 10);
        var checkOut = new DateOnly(2026, 8, 13);

        _hotelRepoMock.Setup(x => x.GetByCityAndDateAsync("GhostCity", checkIn))
            .ReturnsAsync(new List<ProviderHotel>());

        var handler = new GetProviderHotelsQueryHandler(
            _hotelRepoMock.Object, _budgetServiceMock.Object, _mapperMock.Object);

        var query = new GetProviderHotelsQuery
        {
            City = "GhostCity",
            CheckIn = checkIn,
            CheckOut = checkOut,
            PersonCount = 1
        };

        var result = await handler.Handle(query, CancellationToken.None);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetProviderHotels_FiltersUnavailableHotels()
    {
        var checkIn = new DateOnly(2026, 8, 10);
        var checkOut = new DateOnly(2026, 8, 13);
        var availableHotel = CreateProviderHotel("Paris", 100m, true);
        var unavailableHotel = CreateProviderHotel("Paris", 200m, false);

        var hotels = new List<ProviderHotel> { availableHotel, unavailableHotel };

        _hotelRepoMock.Setup(x => x.GetByCityAndDateAsync("Paris", checkIn)).ReturnsAsync(hotels);
        _budgetServiceMock.Setup(x => x.GetSeasonMultiplier(checkIn)).Returns(1.0m);
        _budgetServiceMock.Setup(x => x.SegmentHotel("Paris")).Returns((80m, 200m));
        _mapperMock.Setup(x => x.Map<ProviderHotelResponse>(It.IsAny<ProviderHotel>()))
            .Returns((ProviderHotel h) => new ProviderHotelResponse
            {
                Id = h.Id,
                BasePricePerNight = h.PricePerNight,
            });

        var handler = new GetProviderHotelsQueryHandler(
            _hotelRepoMock.Object, _budgetServiceMock.Object, _mapperMock.Object);

        var query = new GetProviderHotelsQuery
        {
            City = "Paris",
            CheckIn = checkIn,
            CheckOut = checkOut,
            PersonCount = 1
        };

        var result = await handler.Handle(query, CancellationToken.None);
        Assert.Single(result);
    }

    #endregion

    #region Helper Methods

    private static ProviderFlight CreateProviderFlight(string fromCity, string toCity, decimal price, string fromCode = "IST", string toCode = "CDG")
    {
        return new ProviderFlight
        {
            Id = Guid.NewGuid(),
            DepartureCity = fromCity,
            ArrivalCity = toCity,
            DepartureAirportCode = fromCode,
            ArrivalAirportCode = toCode,
            Price = price,
            CurrencyCode = "USD",
            DepartureTime = DateTime.UtcNow,
            ArrivalTime = DateTime.UtcNow.AddHours(3),
            DurationMinutes = 180,
            FlightNumber = "TF" + Random.Shared.Next(100, 999),
            Airline = "TestAir",
            AirlineLogoUrl = null,
            ProviderName = "TestProvider",
            AvailableSeats = 10,
        };
    }

    private static ProviderHotel CreateProviderHotel(string city, decimal pricePerNight, bool isAvailable = true)
    {
        return new ProviderHotel
        {
            Id = Guid.NewGuid(),
            HotelName = $"Hotel {Guid.NewGuid():N}[..8]",
            City = city,
            Country = "FR",
            PricePerNight = pricePerNight,
            CurrencyCode = "USD",
            IsAvailable = isAvailable,
            ProviderName = "TestProvider",
        };
    }

    private static Trip CreateTripWithDestinations(Guid tripId, string origin, string destCity, DateOnly departureDate)
    {
        var destId = Guid.NewGuid();
        var trip = new Trip
        {
            Id = tripId,
            Origin = origin,
            OriginCountry = "TR",
            Status = TripStatus.Draft,
            Destinations = new List<TripDestination>
            {
                new TripDestination(departureDate.AddDays(-3), departureDate, destCity, "FR", 1)
                {
                    Id = destId,
                    TripId = tripId
                }
            }
        };
        return trip;
    }

    #endregion
}
