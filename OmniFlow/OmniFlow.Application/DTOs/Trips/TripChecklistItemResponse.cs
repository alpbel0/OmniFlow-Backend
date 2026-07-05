namespace OmniFlow.Application.DTOs.Trips;

public class TripChecklistItemResponse
{
	public string ItemKey { get; set; } = string.Empty;

	public bool IsConfirmed { get; set; }

	public DateTime? ConfirmedAt { get; set; }
}
