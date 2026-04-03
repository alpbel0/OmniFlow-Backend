using AutoMapper;
using Moq;
using OmniFlow.Application.DTOs.Places;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.Places.Queries.GetPlaceById;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.UnitTests.Places;

public class GetPlaceByIdQueryHandlerTests
{
    private readonly Mock<IPlaceRepositoryAsync> _placeRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly GetPlaceByIdQueryHandler _handler;

    public GetPlaceByIdQueryHandlerTests()
    {
        _placeRepositoryMock = new Mock<IPlaceRepositoryAsync>();
        _mapperMock = new Mock<IMapper>();
        _handler = new GetPlaceByIdQueryHandler(_placeRepositoryMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingPlace_ReturnsPlaceResponse()
    {
        // Arrange
        var placeId = Guid.NewGuid();
        var place = new Place
        {
            Id = placeId,
            Name = "Test Place",
            Category = PlaceCategory.Restaurant,
            City = "Antalya",
            Country = "Turkey",
            Latitude = 36.8,
            Longitude = 30.7
        };

        var expectedResponse = new PlaceResponse
        {
            Id = placeId,
            Name = "Test Place",
            Category = PlaceCategory.Restaurant,
            City = "Antalya",
            Country = "Turkey",
            Latitude = 36.8,
            Longitude = 30.7
        };

        _placeRepositoryMock
            .Setup(x => x.GetByIdAsync(placeId))
            .ReturnsAsync(place);

        _mapperMock
            .Setup(x => x.Map<PlaceResponse>(place))
            .Returns(expectedResponse);

        var query = new GetPlaceByIdQuery(placeId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Test Place");
        result.City.Should().Be("Antalya");
    }

    [Fact]
    public async Task Handle_NonExistingPlace_ThrowsEntityNotFoundException()
    {
        // Arrange
        var placeId = Guid.NewGuid();

        _placeRepositoryMock
            .Setup(x => x.GetByIdAsync(placeId))
            .ReturnsAsync((Place?)null);

        var query = new GetPlaceByIdQuery(placeId);

        // Act
        var act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<EntityNotFoundException>()
            .WithMessage($"Place with id '{placeId}' was not found.");
    }

    [Fact]
    public async Task Handle_WithAllFields_MapsCorrectly()
    {
        // Arrange
        var placeId = Guid.NewGuid();
        var place = new Place
        {
            Id = placeId,
            Name = "Full Place",
            Category = PlaceCategory.Museum,
            City = "Istanbul",
            Country = "Turkey",
            Latitude = 41.0,
            Longitude = 29.0,
            EstimatedPrice = 150,
            IsFree = false,
            Rating = 4.5m,
            DurationMinutes = 90
        };

        var expectedResponse = new PlaceResponse
        {
            Id = placeId,
            Name = "Full Place",
            Category = PlaceCategory.Museum,
            City = "Istanbul",
            Country = "Turkey",
            Latitude = 41.0,
            Longitude = 29.0,
            EstimatedPrice = 150,
            IsFree = false,
            Rating = 4.5m,
            DurationMinutes = 90
        };

        _placeRepositoryMock
            .Setup(x => x.GetByIdAsync(placeId))
            .ReturnsAsync(place);

        _mapperMock
            .Setup(x => x.Map<PlaceResponse>(place))
            .Returns(expectedResponse);

        var query = new GetPlaceByIdQuery(placeId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Category.Should().Be(PlaceCategory.Museum);
        result.EstimatedPrice.Should().Be(150);
        result.Rating.Should().Be(4.5m);
        result.DurationMinutes.Should().Be(90);
    }
}