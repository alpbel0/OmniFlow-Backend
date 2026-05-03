using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.DTOs.Posts;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Application.Features.Posts.Queries.GetTrendingTags;

public class GetTrendingTagsQueryHandler : IRequestHandler<GetTrendingTagsQuery, IReadOnlyList<TrendingTagResponse>>
{
	private const int DefaultLimit = 6;
	private const int MaxLimit = 20;
	private const int DefaultDays = 7;
	private const int MaxDays = 30;
	private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");

	private readonly IApplicationDbContext _context;

	public GetTrendingTagsQueryHandler(IApplicationDbContext context)
	{
		_context = context;
	}

	public async Task<IReadOnlyList<TrendingTagResponse>> Handle(GetTrendingTagsQuery request, CancellationToken cancellationToken)
	{
		var limit = Math.Clamp(request.Limit > 0 ? request.Limit : DefaultLimit, 1, MaxLimit);
		var days = Math.Clamp(request.Days > 0 ? request.Days : DefaultDays, 1, MaxDays);
		var cutoff = DateTime.UtcNow.AddDays(-days);

		var recentPosts = await _context.Posts
			.Where(post => post.DeletedAt == null && post.IsVisible && post.CreatedAt >= cutoff)
			.ToListAsync(cancellationToken);

		var counts = new Dictionary<string, TagAggregate>(StringComparer.Ordinal);

		foreach (var post in recentPosts)
		{
			foreach (var rawTag in post.Tags)
			{
				var displayTag = NormalizeTag(rawTag);
				if (string.IsNullOrWhiteSpace(displayTag))
				{
					continue;
				}

				var key = displayTag.ToLower(TurkishCulture);
				if (string.IsNullOrWhiteSpace(key))
				{
					continue;
				}

				if (counts.TryGetValue(key, out var existing))
				{
					counts[key] = existing with { Count = existing.Count + 1 };
				}
				else
				{
					counts[key] = new TagAggregate(displayTag, 1);
				}
			}
		}

		return counts
			.OrderByDescending(entry => entry.Value.Count)
			.ThenBy(entry => entry.Key)
			.Take(limit)
			.Select(entry => new TrendingTagResponse
			{
				Tag = entry.Value.Tag,
				Count = entry.Value.Count
			})
			.ToList();
	}

	private static string NormalizeTag(string? tag)
	{
		if (string.IsNullOrWhiteSpace(tag))
		{
			return string.Empty;
		}

		var trimmed = tag.Trim();
		if (trimmed.StartsWith('#'))
		{
			trimmed = trimmed[1..].Trim();
		}

		return trimmed;
	}

	private sealed record TagAggregate(string Tag, int Count);
}