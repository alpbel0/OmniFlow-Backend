using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.UnitTests.Phase1;

public class UserEntityTests
{
    [Fact]
    public void NewUser_DefaultRole_IsTraveler()
    {
        var user = new User();
        user.Role.Should().Be(Roles.Traveler);
    }

    [Fact]
    public void NewUser_DefaultKarmaScore_IsZero()
    {
        var user = new User();
        user.KarmaScore.Should().Be(0);
    }

    [Fact]
    public void NewUser_IsVerified_DefaultIsFalse()
    {
        var user = new User();
        user.IsVerified.Should().BeFalse();
    }

    [Fact]
    public void NewUser_IsSuspended_DefaultIsFalse()
    {
        var user = new User();
        user.IsSuspended.Should().BeFalse();
    }

    [Fact]
    public void NewUser_Id_IsNotEmptyGuid()
    {
        var user = new User();
        user.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void NewUser_CreatedAt_IsSet()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var user = new User();
        user.CreatedAt.Should().BeAfter(before);
    }

    [Fact]
    public void NewUser_FollowersCount_DefaultIsZero()
    {
        var user = new User();
        user.FollowersCount.Should().Be(0);
    }

    [Fact]
    public void NewUser_FollowingCount_DefaultIsZero()
    {
        var user = new User();
        user.FollowingCount.Should().Be(0);
    }

    [Fact]
    public void TwoNewUsers_HaveDifferentIds()
    {
        var user1 = new User();
        var user2 = new User();
        user1.Id.Should().NotBe(user2.Id);
    }
}
