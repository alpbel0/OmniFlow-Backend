using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Application.Helpers;

public static class BlockVisibilityHelper
{
	public static Task<bool> HasBlockRelationshipAsync(
		IApplicationDbContext context,
		Guid firstUserId,
		Guid secondUserId,
		CancellationToken cancellationToken = default)
	{
		return context.Blocks.AnyAsync(
			block =>
				(block.BlockerId == firstUserId && block.BlockedUserId == secondUserId) ||
				(block.BlockerId == secondUserId && block.BlockedUserId == firstUserId),
			cancellationToken);
	}

	public static async Task<HashSet<Guid>> GetBlockedUserIdsAsync(
		IApplicationDbContext context,
		Guid currentUserId,
		CancellationToken cancellationToken = default)
	{
		var blockedUserIds = await context.Blocks
			.Where(block => block.BlockerId == currentUserId || block.BlockedUserId == currentUserId)
			.Select(block => block.BlockerId == currentUserId ? block.BlockedUserId : block.BlockerId)
			.ToListAsync(cancellationToken);

		return blockedUserIds.ToHashSet();
	}
}