using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.DTOs.Karma;

public class KarmaEventResponse
{
	public KarmaEventType EventType { get; set; }

	public int Points { get; set; }

	public KarmaSourceType? SourceType { get; set; }

	public DateTime CreatedAt { get; set; }

	public string? ActorUsername { get; set; }
}
