using Moq;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.Admin.Commands.SetUserSuspended;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.UnitTests.Admin;

public class SetUserSuspendedCommandTests
{
	[Fact]
	public async Task Handle_WhenTargetIsAdmin_ThrowsForbiddenException()
	{
		var currentAdminId = Guid.NewGuid();
		var targetAdminId = Guid.NewGuid();
		var users = new List<User>
		{
			new() { Id = currentAdminId, Username = "admin-one", Email = "one@example.com", Role = Roles.Admin },
			new() { Id = targetAdminId, Username = "admin-two", Email = "two@example.com", Role = Roles.Admin }
		};
		var context = new Mock<IApplicationDbContext>();
		context.Setup(x => x.Users).Returns(MockDbSetHelper.CreateAsyncMockDbSet(users).Object);
		var authenticatedUser = new Mock<IAuthenticatedUserService>();
		authenticatedUser.Setup(x => x.UserId).Returns(currentAdminId.ToString());
		var handler = new SetUserSuspendedCommandHandler(context.Object, authenticatedUser.Object);

		var action = () => handler.Handle(
			new SetUserSuspendedCommand { UserId = targetAdminId, IsSuspended = true },
			CancellationToken.None);

		await action.Should().ThrowAsync<ForbiddenException>();
		users.Single(x => x.Id == targetAdminId).IsSuspended.Should().BeFalse();
		context.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
	}
}
