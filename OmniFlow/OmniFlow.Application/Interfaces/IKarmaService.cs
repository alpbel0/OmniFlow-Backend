using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Interfaces;

public interface IKarmaService
{
	Task AwardKarmaAsync(
		Guid userId,
		Guid? actorId,
		KarmaEventType eventType,
		int points,
		Guid? sourceId,
		KarmaSourceType? sourceType);
}
