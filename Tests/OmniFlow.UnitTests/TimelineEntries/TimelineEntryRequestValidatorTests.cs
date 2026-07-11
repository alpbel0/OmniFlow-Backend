using OmniFlow.Application.DTOs.TimelineEntries;
using OmniFlow.Domain.Enums;

namespace OmniFlow.UnitTests.TimelineEntries;

public class TimelineEntryRequestValidatorTests
{
    private readonly CreateTimelineEntryRequestValidator _validator = new();

    [Fact]
    public void Validate_CustomTransportWithValidCoordinates_Passes()
    {
        var request = CreateValidTransportRequest();

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(nameof(CreateTimelineEntryRequest.TransportFromLatitude), -91)]
    [InlineData(nameof(CreateTimelineEntryRequest.TransportFromLatitude), 91)]
    [InlineData(nameof(CreateTimelineEntryRequest.TransportToLatitude), -91)]
    [InlineData(nameof(CreateTimelineEntryRequest.TransportToLatitude), 91)]
    public void Validate_CustomTransportLatitudeOutsideRange_Fails(string propertyName, double value)
    {
        var request = CreateValidTransportRequest();
        SetProperty(request, propertyName, value);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == propertyName);
    }

    [Theory]
    [InlineData(nameof(CreateTimelineEntryRequest.TransportFromLongitude), -181)]
    [InlineData(nameof(CreateTimelineEntryRequest.TransportFromLongitude), 181)]
    [InlineData(nameof(CreateTimelineEntryRequest.TransportToLongitude), -181)]
    [InlineData(nameof(CreateTimelineEntryRequest.TransportToLongitude), 181)]
    public void Validate_CustomTransportLongitudeOutsideRange_Fails(string propertyName, double value)
    {
        var request = CreateValidTransportRequest();
        SetProperty(request, propertyName, value);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == propertyName);
    }

    private static CreateTimelineEntryRequest CreateValidTransportRequest()
    {
        return new CreateTimelineEntryRequest
        {
            TripId = Guid.NewGuid(),
            DestinationId = Guid.NewGuid(),
            DayNumber = 1,
            EntryType = TimelineEntryType.CustomTransport,
            TransportType = TransportMode.Train,
            TransportFromStation = "Roma Termini",
            TransportToStation = "Firenze SMN",
            TransportFromLatitude = 41.901,
            TransportFromLongitude = 12.501,
            TransportToLatitude = 43.776,
            TransportToLongitude = 11.248
        };
    }

    private static void SetProperty(CreateTimelineEntryRequest request, string propertyName, double value)
    {
        var property = typeof(CreateTimelineEntryRequest).GetProperty(propertyName)
            ?? throw new InvalidOperationException($"Unknown property {propertyName}.");
        property.SetValue(request, value);
    }
}
