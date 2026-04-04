using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Services;

public class KarmaService : IKarmaService
{
	private readonly IApplicationDbContext _context;

	public KarmaService(IApplicationDbContext context)
	{
		_context = context;
	}

	public Task AwardKarmaAsync(
		Guid userId,
		Guid? actorId,
		KarmaEventType eventType,
		int points,
		Guid? sourceId,
		KarmaSourceType? sourceType)
	{
		return AwardKarmaInternalAsync(userId, actorId, eventType, points, sourceId, sourceType);
	}

	private async Task AwardKarmaInternalAsync(
		Guid userId,
		Guid? actorId,
		KarmaEventType eventType,
		int points,
		Guid? sourceId,
		KarmaSourceType? sourceType)
	{
		var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
		if (user == null)
		{
			throw new EntityNotFoundException("User", userId);
		}

		var alreadyAwarded = await _context.KarmaEvents.AnyAsync(x =>
			x.UserId == userId &&
			x.SourceId == sourceId &&
			x.EventType == eventType &&
			x.ActorId == actorId);

		if (alreadyAwarded)
		{
			return;
		}

		var karmaEvent = new KarmaEvent
		{
			UserId = userId,
			ActorId = actorId,
			EventType = eventType,
			Points = points,
			SourceId = sourceId,
			SourceType = sourceType
		};

		await _context.KarmaEvents.AddAsync(karmaEvent);
		user.KarmaScore += points;
		await _context.SaveChangesAsync();
	}
}
