using MediatR;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.CommunityTips.Commands.RemoveUpvoteTip;

/// <summary>
/// Handler for removing an upvote from a community tip.
/// Business rules:
/// - Tip must exist
/// - User must have previously upvoted the tip
/// - UpvoteCount decrements (clamped to >= 0)
/// - Karma is revoked for tip owner
/// </summary>
public class RemoveUpvoteTipCommandHandler : IRequestHandler<RemoveUpvoteTipCommand, Unit>
{
	private readonly ICommunityTipRepositoryAsync _tipRepository;
	private readonly IApplicationDbContext _context;
	private readonly IAuthenticatedUserService _authenticatedUserService;
	private readonly IKarmaService _karmaService;

	public RemoveUpvoteTipCommandHandler(
		ICommunityTipRepositoryAsync tipRepository,
		IApplicationDbContext context,
		IAuthenticatedUserService authenticatedUserService,
		IKarmaService karmaService)
	{
		_tipRepository = tipRepository;
		_context = context;
		_authenticatedUserService = authenticatedUserService;
		_karmaService = karmaService;
	}

	public async Task<Unit> Handle(RemoveUpvoteTipCommand request, CancellationToken cancellationToken)
	{
		var tip = await _tipRepository.GetByIdAsync(request.TipId);
		if (tip == null)
		{
			throw new EntityNotFoundException("Tip", request.TipId);
		}

		var userId = Guid.Parse(_authenticatedUserService.UserId);

		var existingUpvote = await _context.TipUpvotes
			.FindAsync(new object[] { request.TipId, userId }, cancellationToken);

		if (existingUpvote == null)
		{
			throw new EntityNotFoundException("TipUpvote", request.TipId);
		}

		_context.TipUpvotes.Remove(existingUpvote);
		tip.UpvoteCount = Math.Max(0, tip.UpvoteCount - 1);
		await _context.SaveChangesAsync(cancellationToken);

		await _karmaService.RevokeKarmaAsync(
			tip.UserId,
			userId,
			KarmaEventType.TipUpvoted,
			tip.Id,
			KarmaSourceType.Tip);

		return Unit.Value;
	}
}