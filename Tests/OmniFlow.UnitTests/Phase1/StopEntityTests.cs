using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.UnitTests.Phase1;

public class StopEntityTests
{
    [Fact]
    public void NewStop_DefaultAddedBy_IsUser()
    {
        var stop = new Stop();
        stop.AddedBy.Should().Be(StopAddedBy.User);
    }

    [Fact]
    public void NewStop_IsVisited_DefaultIsFalse()
    {
        var stop = new Stop();
        stop.IsVisited.Should().BeFalse();
    }

    [Fact]
    public void NewStop_IsTimeLocked_DefaultIsFalse()
    {
        var stop = new Stop();
        stop.IsTimeLocked.Should().BeFalse();
    }

    [Fact]
    public void NewStop_ActivityPrice_DefaultIsZero()
    {
        var stop = new Stop();
        stop.ActivityPrice.Should().Be(0);
    }

    [Fact]
    public void NewStop_TransportPrice_DefaultIsZero()
    {
        var stop = new Stop();
        stop.TransportPrice.Should().Be(0);
    }

    [Fact]
    public void NewStop_VisitedAt_DefaultIsNull()
    {
        var stop = new Stop();
        stop.VisitedAt.Should().BeNull();
    }

    [Fact]
    public void NewStop_AiReasoning_DefaultIsNull()
    {
        var stop = new Stop();
        stop.AiReasoning.Should().BeNull();
    }

    [Fact]
    public void NewStop_Id_IsNotEmptyGuid()
    {
        var stop = new Stop();
        stop.Id.Should().NotBe(Guid.Empty);
    }
}
