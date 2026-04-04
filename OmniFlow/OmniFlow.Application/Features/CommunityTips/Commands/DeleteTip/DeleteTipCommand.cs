using MediatR;

namespace OmniFlow.Application.Features.CommunityTips.Commands.DeleteTip;

public class DeleteTipCommand : IRequest<Unit>
{
	public Guid TipId { get; set; }
}
