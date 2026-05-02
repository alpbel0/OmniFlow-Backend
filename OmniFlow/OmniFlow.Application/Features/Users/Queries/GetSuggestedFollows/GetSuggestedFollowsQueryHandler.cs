using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.DTOs.Users;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Application.Features.Users.Queries.GetSuggestedFollows;

public class GetSuggestedFollowsQueryHandler : IRequestHandler<GetSuggestedFollowsQuery, IReadOnlyList<SuggestedFollowResponse>>
{
	private const int MinLimit = 1;
	private const int MaxLimit = 50;

	private readonly IApplicationDbContext _context;
	private readonly Guid _currentUserId;

	public GetSuggestedFollowsQueryHandler(IApplicationDbContext context, IAuthenticatedUserService authenticatedUserService)
	{
		_context = context;
		_currentUserId = Guid.Parse(authenticatedUserService.UserId);
	}

	public async Task<IReadOnlyList<SuggestedFollowResponse>> Handle(
		GetSuggestedFollowsQuery request,
		CancellationToken cancellationToken)
	{
		var limit = Math.Clamp(request.Limit, MinLimit, MaxLimit);
		var excludeIds = request.ExcludeUserIds?.ToHashSet() ?? new HashSet<Guid>();
		var reasonMap = new Dictionary<Guid, string>();

	// ─── Yardımcı: mevcut kullanıcının takip ettiği kişiler ────────────────────
		var currentUserFollowingIds = await _context.Follows
			.AsNoTracking()
			.Where(f => f.FollowerId == _currentUserId)
			.Select(f => f.FollowingId)
			.ToListAsync(cancellationToken);
		var currentUserFollowingSet = currentUserFollowingIds.ToHashSet();

		// ─── Tier 1: Paylaşımlarını beğendin ───────────────────────────────────
		var upvotedPostIds = await _context.PostUpvotes
			.AsNoTracking()
			.Where(pu => pu.UserId == _currentUserId)
			.Select(pu => pu.PostId)
			.ToListAsync(cancellationToken);

		var upvotedOwnerIds = await _context.Posts
			.AsNoTracking()
			.Where(p => upvotedPostIds.Contains(p.Id))
			.Select(p => p.UserId)
			.Distinct()
			.ToListAsync(cancellationToken);

		var commentedPostIds = await _context.Comments
			.AsNoTracking()
			.Where(c => c.UserId == _currentUserId)
			.Select(c => c.PostId)
			.ToListAsync(cancellationToken);

		var commentedOwnerIds = await _context.Posts
			.AsNoTracking()
			.Where(p => commentedPostIds.Contains(p.Id))
			.Select(p => p.UserId)
			.Distinct()
			.ToListAsync(cancellationToken);

		var tier1Ids = upvotedOwnerIds
			.Concat(commentedOwnerIds)
			.Distinct()
			.Where(id => id != _currentUserId
				&& !excludeIds.Contains(id)
				&& !currentUserFollowingSet.Contains(id))
			.ToList();

		foreach (var id in tier1Ids)
			reasonMap[id] = "Paylaşımlarını beğendin";

		// ─── Tier 2: Seni takip ediyor ─────────────────────────────────────────
		var tier2Ids = await _context.Follows
			.AsNoTracking()
			.Where(f => f.FollowingId == _currentUserId)
			.Where(f => f.FollowerId != _currentUserId)
			.Where(f => !_context.Follows.Any(f2 =>
				f2.FollowerId == _currentUserId && f2.FollowingId == f.FollowerId))
			.Where(f => !excludeIds.Contains(f.FollowerId))
			.Where(f => !currentUserFollowingSet.Contains(f.FollowerId))
			.Select(f => f.FollowerId)
			.ToListAsync(cancellationToken);

		tier2Ids = tier2Ids.Where(id => !reasonMap.ContainsKey(id)).ToList();

		foreach (var id in tier2Ids)
			reasonMap[id] = "Seni takip ediyor";

		// ─── Tier 3: Popüler gezgin ─────────────────────────────────────────────
		var alreadySuggested = tier1Ids
			.Concat(tier2Ids)
			.Concat(excludeIds)
			.Concat(new[] { _currentUserId })
			.ToHashSet();

		var tier1Plus2Count = tier1Ids.Count + tier2Ids.Count;
		var remainingSlots = limit - tier1Plus2Count;
		var tier3Ids = await _context.Users
			.AsNoTracking()
			.Where(u => !u.IsSuspended)
			.Where(u => !alreadySuggested.Contains(u.Id))
			.Where(u => !_context.Blocks.Any(b =>
				(b.BlockerId == _currentUserId && b.BlockedUserId == u.Id) ||
				(b.BlockedUserId == _currentUserId && b.BlockerId == u.Id)))
			.OrderByDescending(u => u.KarmaScore)
			.ThenBy(u => u.Username)
			.Take(remainingSlots > 0 ? remainingSlots : 0)
			.Select(u => u.Id)
			.ToListAsync(cancellationToken);

		foreach (var id in tier3Ids)
			reasonMap[id] = "Popüler gezgin";

		// ─── Build final list ──────────────────────────────────────────────────
		var allSuggestedIds = tier1Ids
			.Concat(tier2Ids)
			.Concat(tier3Ids)
			.Distinct()
			.ToList();

		if (allSuggestedIds.Count == 0)
			return Array.Empty<SuggestedFollowResponse>();

		var userDetails = await _context.Users
			.AsNoTracking()
			.Where(u => allSuggestedIds.Contains(u.Id))
			.Select(u => new
			{
				u.Id,
				u.Username,
				u.ProfilePhotoUrl,
				u.KarmaScore
			})
			.ToListAsync(cancellationToken);

		var tripCountsRaw = await _context.Trips
			.AsNoTracking()
			.Where(t => allSuggestedIds.Contains(t.OwnerId) && t.DeletedAt == null)
			.GroupBy(t => t.OwnerId)
			.Select(g => new { OwnerId = g.Key, Count = g.Count() })
			.ToListAsync(cancellationToken);
		var tripCounts = tripCountsRaw.ToDictionary(x => x.OwnerId, x => x.Count);

		var followingIds = await _context.Follows
			.AsNoTracking()
			.Where(f => f.FollowerId == _currentUserId && allSuggestedIds.Contains(f.FollowingId))
			.Select(f => f.FollowingId)
			.ToListAsync(cancellationToken);
		var followingSet = followingIds.ToHashSet();

		return userDetails
			.Select(u => new SuggestedFollowResponse
			{
				Id = u.Id,
				Username = u.Username,
				ProfilePhotoUrl = u.ProfilePhotoUrl,
				TripCount = tripCounts.GetValueOrDefault(u.Id, 0),
				KarmaScore = u.KarmaScore,
				IsFollowing = followingSet.Contains(u.Id),
				SuggestionReason = reasonMap[u.Id]
			})
			.ToList();
	}
}
