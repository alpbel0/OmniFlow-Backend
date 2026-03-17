using OmniFlow.Domain.Exceptions;

namespace OmniFlow.UnitTests.Phase1;

public class ExceptionTests
{
    [Fact]
    public void DuplicateUpvoteException_Message_ContainsContentType()
    {
        const string contentType = "post";
        var contentId = Guid.NewGuid();

        var ex = new DuplicateUpvoteException(contentType, contentId);

        ex.Message.Should().Contain(contentType);
    }

    [Fact]
    public void DuplicateUpvoteException_Message_ContainsContentId()
    {
        const string contentType = "trip";
        var contentId = Guid.NewGuid();

        var ex = new DuplicateUpvoteException(contentType, contentId);

        ex.Message.Should().Contain(contentId.ToString());
    }

    [Fact]
    public void SelfFollowException_Message_ContainsUserId()
    {
        var userId = Guid.NewGuid();

        var ex = new SelfFollowException(userId);

        ex.Message.Should().Contain(userId.ToString());
    }

    [Fact]
    public void SelfForkException_Message_ContainsTripId()
    {
        var userId = Guid.NewGuid();
        var tripId = Guid.NewGuid();

        var ex = new SelfForkException(userId, tripId);

        ex.Message.Should().Contain(tripId.ToString());
    }

    [Fact]
    public void SelfForkException_Message_ContainsUserId()
    {
        var userId = Guid.NewGuid();
        var tripId = Guid.NewGuid();

        var ex = new SelfForkException(userId, tripId);

        ex.Message.Should().Contain(userId.ToString());
    }

    [Fact]
    public void DuplicateUpvoteException_IsException()
    {
        var ex = new DuplicateUpvoteException("comment", Guid.NewGuid());
        ex.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void SelfFollowException_IsException()
    {
        var ex = new SelfFollowException(Guid.NewGuid());
        ex.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void SelfForkException_IsException()
    {
        var ex = new SelfForkException(Guid.NewGuid(), Guid.NewGuid());
        ex.Should().BeAssignableTo<Exception>();
    }
}
