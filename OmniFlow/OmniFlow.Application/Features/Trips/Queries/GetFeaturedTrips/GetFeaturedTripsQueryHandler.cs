using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.Trips.Queries.GetFeaturedTrips;

public class GetFeaturedTripsQueryHandler : IRequestHandler<GetFeaturedTripsQuery, IReadOnlyList<FeaturedTripResponse>>
{
	private const int MinLimit = 1;
	private const int MaxLimit = 12;

	private readonly IApplicationDbContext _context;

	public GetFeaturedTripsQueryHandler(IApplicationDbContext context)
	{
		_context = context;
	}

	public async Task<IReadOnlyList<FeaturedTripResponse>> Handle(
		GetFeaturedTripsQuery request,
		CancellationToken cancellationToken)
	{
		var limit = Math.Clamp(request.Limit, MinLimit, MaxLimit);
		var cutoff = DateTime.UtcNow.AddDays(-7);

		var recentTrips = await CreateFeaturedTripsQuery(cutoff)
			.Take(limit)
			.ToListAsync(cancellationToken);

		if (recentTrips.Count > 0)
			return recentTrips;

		return await CreateFeaturedTripsQuery(cutoff: null)
			.Take(limit)
			.ToListAsync(cancellationToken);
	}

	private IQueryable<FeaturedTripResponse> CreateFeaturedTripsQuery(DateTime? cutoff)
	{
		var query = _context.Trips
			.AsNoTracking()
			.Where(t =>
				t.Status == TripStatus.Published &&
				t.DeletedAt == null);

		if (cutoff.HasValue)
			query = query.Where(t => t.CreatedAt >= cutoff.Value);

		return query
			.OrderByDescending(t => (t.ForkCount + t.UpvoteCount) * 2 + t.ViewCount)
			.ThenByDescending(t => t.PopularityScore)
			.ThenByDescending(t => t.Id)
			.Select(t => new FeaturedTripResponse
			{
				Id = t.Id,
				Title = t.Title,
				CoverPhotoUrl = t.CoverPhotoUrl,
				Origin = t.Origin,
				OriginCountry = t.OriginCountry,
				ForkCount = t.ForkCount,
				UpvoteCount = t.UpvoteCount,
				PopularityScore = t.PopularityScore,
				StartDate = t.StartDate,
				EndDate = t.EndDate,
				OwnerId = t.OwnerId,
				OwnerUsername = t.Owner != null ? t.Owner.Username : string.Empty,
				OwnerProfilePhotoUrl = t.Owner != null ? t.Owner.ProfilePhotoUrl : null
			});
	}
}
