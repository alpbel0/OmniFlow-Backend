using System.Text;
using System.Text.Json;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.Trips.Queries.ExploreTrips;

public class ExploreTripsQueryHandler : IRequestHandler<ExploreTripsQuery, ExploreTripsViewModel>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthenticatedUserService _authenticatedUserService;
    private readonly IMapper _mapper;

    public ExploreTripsQueryHandler(
        IApplicationDbContext context,
        IAuthenticatedUserService authenticatedUserService,
        IMapper mapper)
    {
        _context = context;
        _authenticatedUserService = authenticatedUserService;
        _mapper = mapper;
    }

    public async Task<ExploreTripsViewModel> Handle(ExploreTripsQuery request, CancellationToken cancellationToken)
    {
        var parameter = request.Parameter;

        // 1. Parse cursor if provided
        CursorInfo? cursorInfo = null;
        if (!string.IsNullOrEmpty(parameter.Cursor))
        {
            try
            {
                var decodedJson = Base64Decode(parameter.Cursor);
                cursorInfo = JsonSerializer.Deserialize<CursorInfo>(decodedJson);
            }
            catch
            {
                // Invalid cursor - treat as no cursor
                cursorInfo = null;
            }
        }

        // 2. Get current user ID (if authenticated)
        Guid? userId = null;
        if (!string.IsNullOrEmpty(_authenticatedUserService.UserId))
        {
            userId = Guid.Parse(_authenticatedUserService.UserId);
        }

        // 3. Build base query: Published + non-soft-deleted
        var query = _context.Trips
            .Include(t => t.Owner)
            .Where(t => t.Status == TripStatus.Published && t.DeletedAt == null);

        // 4. Apply filters
        if (!string.IsNullOrEmpty(parameter.City))
            query = query.Where(t => t.City.ToLower() == parameter.City.ToLower());

        if (!string.IsNullOrEmpty(parameter.Country))
            query = query.Where(t => t.Country.ToLower() == parameter.Country.ToLower());

        if (!string.IsNullOrWhiteSpace(parameter.SearchTerm))
        {
            var searchTerm = parameter.SearchTerm.Trim().ToLower();
            query = query.Where(t =>
                t.Title.ToLower().Contains(searchTerm) ||
                (t.Description != null && t.Description.ToLower().Contains(searchTerm)) ||
                t.City.ToLower().Contains(searchTerm) ||
                t.Country.ToLower().Contains(searchTerm) ||
                (t.Owner != null && t.Owner.Username.ToLower().Contains(searchTerm)) ||
                t.Tags.Any(tag => tag.ToLower().Contains(searchTerm)));
        }

        if (parameter.BudgetTier.HasValue)
            query = query.Where(t => t.BudgetTier == parameter.BudgetTier.Value);

        if (parameter.TravelStyle.HasValue)
            query = query.Where(t => t.TravelStyle == parameter.TravelStyle.Value);

        if (parameter.Tags != null && parameter.Tags.Any())
            query = query.Where(t => t.Tags.Any(tag => parameter.Tags.Contains(tag)));

        // 5. Apply sorting and cursor pagination
        var pageSize = parameter.PageSize;
        var sortBy = parameter.SortBy?.ToLower() ?? "popularity_score";

        if (sortBy == "created_at")
        {
            query = query.OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id);

            // Cursor for created_at sorting (using CreatedAt ticks + Id)
            if (cursorInfo != null && cursorInfo.CreatedAtTicks.HasValue)
            {
                var cursorTicks = cursorInfo.CreatedAtTicks.Value;
                var cursorId = cursorInfo.Id;
                query = query.Where(t =>
                    t.CreatedAt.Ticks < cursorTicks ||
                    (t.CreatedAt.Ticks == cursorTicks && t.Id < cursorId));
            }
        }
        else
        {
            // Default: Sort by popularity_score DESC, then by Id DESC for deterministic ordering
            query = query.OrderByDescending(t => t.PopularityScore).ThenByDescending(t => t.Id);

            // Apply cursor
            if (cursorInfo != null)
            {
                var cursorScore = decimal.Parse(cursorInfo.PopularityScore);
                var cursorId = cursorInfo.Id;
                query = query.Where(t =>
                    t.PopularityScore < cursorScore ||
                    (t.PopularityScore == cursorScore && t.Id < cursorId));
            }
        }

        // 6. Fetch one extra to check if there's more
        var items = await query.Take(pageSize + 1).ToListAsync(cancellationToken);

        var hasMore = items.Count > pageSize;
        var resultItems = hasMore ? items.Take(pageSize).ToList() : items;
        var lastItem = resultItems.LastOrDefault();

        // 7. Map to response
        var tripResponses = _mapper.Map<List<TripResponse>>(resultItems);

        // 8. Set IsUpvoted and IsSaved using BATCH queries (NO N+1)
        if (userId.HasValue)
        {
            var tripIds = resultItems.Select(t => t.Id).ToList();

            // BATCH QUERY - Single query for all upvoted trips
            var upvotedTripIds = await _context.TripUpvotes
                .Where(u => u.UserId == userId.Value && tripIds.Contains(u.TripId))
                .Select(u => u.TripId)
                .ToListAsync(cancellationToken);

            // BATCH QUERY - Single query for all saved trips
            var savedTripIds = await _context.SavedTrips
                .Where(s => s.UserId == userId.Value && tripIds.Contains(s.TripId))
                .Select(s => s.TripId)
                .ToListAsync(cancellationToken);

            // In-memory mapping
            foreach (var response in tripResponses)
            {
                response.IsUpvoted = upvotedTripIds.Contains(response.Id);
                response.IsSaved = savedTripIds.Contains(response.Id);
            }
        }
        else
        {
            // Unauthenticated - set to null
            foreach (var response in tripResponses)
            {
                response.IsUpvoted = null;
                response.IsSaved = null;
            }
        }

        // 9. Generate next cursor
        string? nextCursor = null;
        if (hasMore && lastItem != null)
        {
            var nextCursorInfo = sortBy == "created_at"
                ? new CursorInfo
                {
                    PopularityScore = "0",
                    Id = lastItem.Id,
                    CreatedAtTicks = lastItem.CreatedAt.Ticks
                }
                : new CursorInfo
                {
                    PopularityScore = lastItem.PopularityScore.ToString(), // String preserves precision
                    Id = lastItem.Id
                };
            nextCursor = Base64Encode(JsonSerializer.Serialize(nextCursorInfo));
        }

        return new ExploreTripsViewModel
        {
            Data = tripResponses.AsReadOnly(),
            NextCursor = nextCursor,
            HasMore = hasMore
        };
    }

    private static string Base64Encode(string plainText)
    {
        var bytes = Encoding.UTF8.GetBytes(plainText);
        return Convert.ToBase64String(bytes);
    }

    private static string Base64Decode(string base64Text)
    {
        var bytes = Convert.FromBase64String(base64Text);
        return Encoding.UTF8.GetString(bytes);
    }

}

internal class CursorInfo
{
    public string PopularityScore { get; set; } = "0";
    public Guid Id { get; set; }
    public long? CreatedAtTicks { get; set; }
}
