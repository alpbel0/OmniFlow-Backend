using MediatR;

namespace OmniFlow.Application.Features.Trips.Commands.ToggleChecklistItem;

public class ToggleChecklistItemCommand : IRequest<Unit>
{
	public Guid TripId { get; set; }

	public string ItemKey { get; set; } = string.Empty;

	public bool IsConfirmed { get; set; }
}
