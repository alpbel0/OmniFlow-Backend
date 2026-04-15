using System.Text;
using System.Text.Json;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.DTOs.Posts;
using OmniFlow.Application.Helpers;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.Posts.Queries.GetFeed;

public class GetFeedQueryHandler : IRequestHandler<GetFeedQuery, GetFeedViewModel>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthenticatedUserService _authenticatedUserService;
    private readonly IMapper _mapper;

    public GetFeedQueryHandler(
        IApplicationDbContext context,
        IAuthenticatedUserService authenticatedUserService,
        IMapper mapper)
    {
        _context = context;
        _authenticatedUserService = authenticatedUserService;
        _mapper = mapper;
    }

    public async Task<GetFeedViewModel> Handle(GetFeedQuery request, CancellationToken cancellationToken)
    {
        var parameter = request.Parameter;
        var pageSize = parameter.PageSize > 0 ? parameter.PageSize : 20;
        var cursorInfo = ParseCursor(parameter.Cursor);

        Guid? currentUserId = null;
        if (Guid.TryParse(_authenticatedUserService.UserId, out var parsedUserId))
        {
            currentUserId = parsedUserId;
        }

        IQueryable<Post> query = _context.Posts
            .Include(post => post.User)
            .Where(post => post.DeletedAt == null && post.IsVisible);

        if (currentUserId.HasValue)
        {
            var blockedUserIds = await BlockVisibilityHelper.GetBlockedUserIdsAsync(_context, currentUserId.Value, cancellationToken);
            if (blockedUserIds.Count > 0)
            {
                var blockedUserIdList = blockedUserIds.ToList();
                query = query.Where(post => !blockedUserIdList.Contains(post.UserId));
            }
        }

        if (parameter.Tab == FeedTab.Following)
        {
            if (!currentUserId.HasValue)
            {
                return new GetFeedViewModel();
            }

            var followingIds = await _context.Follows
                .Where(follow => follow.FollowerId == currentUserId.Value)
                .Select(follow => follow.FollowingId)
                .ToListAsync(cancellationToken);

            if (!followingIds.Any())
            {
                return new GetFeedViewModel();
            }

            query = query.Where(post => followingIds.Contains(post.UserId));
        }

        query = query.OrderByDescending(post => post.CreatedAt).ThenByDescending(post => post.Id);

        if (cursorInfo != null)
        {
            var cursorCreatedAtTicks = cursorInfo.CreatedAtTicks;
            var cursorId = cursorInfo.Id;

            query = query.Where(post =>
                post.CreatedAt.Ticks < cursorCreatedAtTicks ||
                (post.CreatedAt.Ticks == cursorCreatedAtTicks && post.Id.CompareTo(cursorId) < 0));
        }

        var items = await query
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = items.Count > pageSize;
        var resultItems = hasMore ? items.Take(pageSize).ToList() : items;
        var mappedPosts = _mapper.Map<List<PostResponse>>(resultItems);

        if (currentUserId.HasValue && resultItems.Any())
        {
            var postIds = resultItems.Select(post => post.Id).ToList();
            var upvotedPostIds = await _context.PostUpvotes
                .Where(upvote => upvote.UserId == currentUserId.Value && postIds.Contains(upvote.PostId))
                .Select(upvote => upvote.PostId)
                .ToListAsync(cancellationToken);

            foreach (var post in mappedPosts)
            {
                post.IsUpvoted = upvotedPostIds.Contains(post.Id);
            }
        }

        var nextCursor = hasMore && resultItems.Any()
            ? EncodeCursor(new FeedCursorInfo
            {
                CreatedAtTicks = resultItems.Last().CreatedAt.Ticks,
                Id = resultItems.Last().Id
            })
            : null;

        return new GetFeedViewModel
        {
            Data = mappedPosts.AsReadOnly(),
            NextCursor = nextCursor,
            HasMore = hasMore
        };
    }

    private static FeedCursorInfo? ParseCursor(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return null;
        }

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            return JsonSerializer.Deserialize<FeedCursorInfo>(json);
        }
        catch
        {
            return null;
        }
    }

    private static string EncodeCursor(FeedCursorInfo cursorInfo)
    {
        var json = JsonSerializer.Serialize(cursorInfo);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }
}

internal sealed class FeedCursorInfo
{
    public long CreatedAtTicks { get; set; }
    public Guid Id { get; set; }
}