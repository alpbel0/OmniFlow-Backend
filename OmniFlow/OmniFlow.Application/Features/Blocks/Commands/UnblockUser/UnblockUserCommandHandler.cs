using MediatR;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Application.Features.Blocks.Commands.UnblockUser;

public class UnblockUserCommandHandler : IRequestHandler<UnblockUserCommand, Unit>
{
	private readonly IApplicationDbContext _context;
	private readonly IAuthenticatedUserService _authenticatedUserService;

	public UnblockUserCommandHandler(
		IApplicationDbContext context,
		IAuthenticatedUserService authenticatedUserService)
	{
		_context = context;
		_authenticatedUserService = authenticatedUserService;
	}

	public async Task<Unit> Handle(UnblockUserCommand request, CancellationToken cancellationToken)
	{
		var currentUserId = Guid.Parse(_authenticatedUserService.UserId);
		var existingBlock = _context.Blocks.FirstOrDefault(block => block.BlockerId == currentUserId && block.BlockedUserId == request.UserId);

		if (existingBlock == null)
		{
			return Unit.Value;
		}

		_context.Blocks.Remove(existingBlock);
		await _context.SaveChangesAsync(cancellationToken);
		return Unit.Value;
	}
}