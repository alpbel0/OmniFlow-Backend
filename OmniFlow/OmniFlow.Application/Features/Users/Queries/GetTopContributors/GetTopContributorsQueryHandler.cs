using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.DTOs.Users;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Application.Features.Users.Queries.GetTopContributors;

public class GetTopContributorsQueryHandler : IRequestHandler<GetTopContributorsQuery, IReadOnlyList<TopContributorResponse>>
{
	private const int MinLimit = 1;
	private const int MaxLimit = 50;

	private readonly IApplicationDbContext _context;

	public GetTopContributorsQueryHandler(IApplicationDbContext context)
	{
		_context = context;
	}

	public async Task<IReadOnlyList<TopContributorResponse>> Handle(
		GetTopContributorsQuery request,
		CancellationToken cancellationToken)
	{
		var limit = Math.Clamp(request.Limit, MinLimit, MaxLimit);

		var users = await _context.Users
			.AsNoTracking()
			.Where(u => u.DeletedAt == null && !u.IsSuspended)
			.OrderByDescending(u => u.KarmaScore)
			.ThenBy(u => u.Username)
			.Take(limit)
			.Select(u => new
			{
				u.Id,
				u.Username,
				u.ProfilePhotoUrl,
				u.KarmaScore
			})
			.ToListAsync(cancellationToken);

		if (users.Count == 0)
			return Array.Empty<TopContributorResponse>();

		var userIds = users.Select(u => u.Id).ToList();

		var tripCounts = await _context.Trips
			.AsNoTracking()
			.Where(t => userIds.Contains(t.OwnerId) && t.DeletedAt == null)
			.GroupBy(t => t.OwnerId)
			.Select(g => new { OwnerId = g.Key, Count = g.Count() })
			.ToDictionaryAsync(x => x.OwnerId, x => x.Count, cancellationToken);

		return users
			.Select(u => new TopContributorResponse
			{
				Id = u.Id,
				Username = u.Username,
				ProfilePhotoUrl = u.ProfilePhotoUrl,
				KarmaScore = u.KarmaScore,
				TripCount = tripCounts.GetValueOrDefault(u.Id, 0)
			})
			.ToList();
	}
}
