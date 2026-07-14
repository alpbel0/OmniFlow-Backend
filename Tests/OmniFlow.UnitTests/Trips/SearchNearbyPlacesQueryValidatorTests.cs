using FluentAssertions;
using OmniFlow.Application.Features.Trips.Queries.SearchNearbyPlaces;

namespace OmniFlow.UnitTests.Trips;

public sealed class SearchNearbyPlacesQueryValidatorTests
{
    private readonly SearchNearbyPlacesQueryValidator _validator = new();

    [Fact]
    public async Task Validate_AcceptsSupportedRadiusAndCoordinateBounds()
    {
        var query = CreateValidQuery() with { Latitude = -90, Longitude = 180, RadiusKm = 5 };

        var result = await _validator.ValidateAsync(query);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(10)]
    public async Task Validate_RejectsUnsupportedRadius(int radiusKm)
    {
        var result = await _validator.ValidateAsync(CreateValidQuery() with { RadiusKm = radiusKm });

        result.Errors.Should().Contain(error => error.PropertyName == nameof(SearchNearbyPlacesQuery.RadiusKm));
    }

    [Theory]
    [InlineData(-90.01, 0)]
    [InlineData(90.01, 0)]
    [InlineData(0, -180.01)]
    [InlineData(0, 180.01)]
    public async Task Validate_RejectsCoordinatesOutsideWorldBounds(double latitude, double longitude)
    {
        var result = await _validator.ValidateAsync(CreateValidQuery() with
        {
            Latitude = latitude,
            Longitude = longitude
        });

        result.IsValid.Should().BeFalse();
    }

    private static SearchNearbyPlacesQuery CreateValidQuery() => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        41.0082,
        28.9784,
        3,
        NearbyPlaceCategoryGroup.All);
}
