using MediatR;

namespace OmniFlow.Application.Features.CommunityTips.Commands.CreateTip;

public class CreateTipCommand : IRequest<Guid>
{
	public Guid TripId { get; set; }

	public Guid? PlaceId { get; set; }

	public string Content { get; set; } = string.Empty;
}
