using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities;

public class TripChecklistConfirmation : BaseEntity
{
	public Guid TripId { get; set; }

	public string ItemKey { get; set; } = string.Empty;

	public bool IsConfirmed { get; set; }

	public DateTime? ConfirmedAt { get; set; }

	public Trip? Trip { get; set; }
}
