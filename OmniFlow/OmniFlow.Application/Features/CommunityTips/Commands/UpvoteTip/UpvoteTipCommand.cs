using MediatR;

namespace OmniFlow.Application.Features.CommunityTips.Commands.UpvoteTip;

public class UpvoteTipCommand : IRequest<Unit>
{
	public Guid TipId { get; set; }
}
