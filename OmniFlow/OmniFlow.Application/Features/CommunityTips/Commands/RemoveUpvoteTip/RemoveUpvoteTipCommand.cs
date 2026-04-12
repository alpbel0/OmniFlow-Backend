using MediatR;

namespace OmniFlow.Application.Features.CommunityTips.Commands.RemoveUpvoteTip;

/// <summary>
/// Command to remove an upvote from a community tip.
/// </summary>
public class RemoveUpvoteTipCommand : IRequest<Unit>
{
	public Guid TipId { get; set; }
}