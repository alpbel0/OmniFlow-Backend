using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.UnitTests.Phase1;

public class TripEntityTests
{
    [Fact]
    public void NewTrip_DefaultStatus_IsDraft()
    {
        var trip = new Trip();
        trip.Status.Should().Be(TripStatus.Draft);
    }

    [Fact]
    public void NewTrip_ForkCount_DefaultIsZero()
    {
        var trip = new Trip();
        trip.ForkCount.Should().Be(0);
    }

    [Fact]
    public void NewTrip_UpvoteCount_DefaultIsZero()
    {
        var trip = new Trip();
        trip.UpvoteCount.Should().Be(0);
    }

    [Fact]
    public void NewTrip_ViewCount_DefaultIsZero()
    {
        var trip = new Trip();
        trip.ViewCount.Should().Be(0);
    }

    [Fact]
    public void NewTrip_PopularityScore_DefaultIsZero()
    {
        var trip = new Trip();
        trip.PopularityScore.Should().Be(0);
    }

    [Fact]
    public void NewTrip_PersonCount_DefaultIsOne()
    {
        var trip = new Trip();
        trip.PersonCount.Should().Be(1);
    }

    [Fact]
    public void NewTrip_Id_IsNotEmptyGuid()
    {
        var trip = new Trip();
        trip.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void NewTrip_Tags_DefaultIsEmptyList()
    {
        var trip = new Trip();
        trip.Tags.Should().NotBeNull();
        trip.Tags.Should().BeEmpty();
    }

    [Fact]
    public void NewTrip_ForkedFromId_DefaultIsNull()
    {
        var trip = new Trip();
        trip.ForkedFromId.Should().BeNull();
    }

    [Fact]
    public void SetOriginCoordinates_WithValues_SetsLatitudeAndLongitude()
    {
        var trip = new Trip();

        trip.SetOriginCoordinates(41.0082, 28.9784);

        trip.OriginLatitude.Should().Be(41.0082);
        trip.OriginLongitude.Should().Be(28.9784);
    }

    [Fact]
    public void SetOriginCoordinates_WithNulls_ClearsLatitudeAndLongitude()
    {
        var trip = new Trip();
        trip.SetOriginCoordinates(41.0082, 28.9784);

        trip.SetOriginCoordinates(null, null);

        trip.OriginLatitude.Should().BeNull();
        trip.OriginLongitude.Should().BeNull();
    }
}
