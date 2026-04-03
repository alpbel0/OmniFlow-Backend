using AutoMapper;
using Moq;
using OmniFlow.Application.Features.Places.Commands.CreatePlace;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.UnitTests.Places;

public class CreatePlaceCommandHandlerTests
{
    private readonly Mock<IPlaceRepositoryAsync> _placeRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly CreatePlaceCommandHandler _handler;

    public CreatePlaceCommandHandlerTests()
    {
        _placeRepositoryMock = new Mock<IPlaceRepositoryAsync>();
        _mapperMock = new Mock<IMapper>();
        _handler = new CreatePlaceCommandHandler(_placeRepositoryMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsPlaceId()
    {
        // Arrange
        var command = new CreatePlaceCommand
        {
            Name = "Test Place",
            Category = PlaceCategory.Restaurant,
            City = "Antalya",
            Country = "Turkey",
            Latitude = 36.8,
            Longitude = 30.7
        };

        var mappedPlace = new Place
        {
            Id = Guid.NewGuid(),
            Name = "Test Place",
            Category = PlaceCategory.Restaurant,
            City = "Antalya",
            Country = "Turkey",
            Latitude = 36.8,
            Longitude = 30.7
        };

        _mapperMock
            .Setup(x => x.Map<Place>(command))
            .Returns(mappedPlace);

        _placeRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Place>()))
            .ReturnsAsync(mappedPlace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(mappedPlace.Id);
        _placeRepositoryMock.Verify(x => x.AddAsync(It.Is<Place>(p => p.IsActive)), Times.Once);
    }

    [Fact]
    public async Task Handle_WithAllFields_MapsAndAddsCorrectly()
    {
        // Arrange
        var command = new CreatePlaceCommand
        {
            Name = "Full Place",
            Description = "Description",
            Category = PlaceCategory.Museum,
            City = "Istanbul",
            Country = "Turkey",
            Latitude = 41.0,
            Longitude = 29.0,
            EstimatedPrice = 100,
            IsFree = false,
            DurationMinutes = 120,
            Rating = 4.5m
        };

        var mappedPlace = new Place
        {
            Id = Guid.NewGuid(),
            Name = "Full Place",
            Description = "Description",
            Category = PlaceCategory.Museum,
            City = "Istanbul",
            Country = "Turkey",
            Latitude = 41.0,
            Longitude = 29.0,
            EstimatedPrice = 100,
            IsFree = false,
            DurationMinutes = 120,
            Rating = 4.5m
        };

        _mapperMock
            .Setup(x => x.Map<Place>(command))
            .Returns(mappedPlace);

        _placeRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Place>()))
            .ReturnsAsync(mappedPlace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(mappedPlace.Id);
        _mapperMock.Verify(x => x.Map<Place>(command), Times.Once);
    }
}