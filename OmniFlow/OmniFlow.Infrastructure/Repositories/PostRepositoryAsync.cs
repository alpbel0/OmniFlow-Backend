using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Application.Parameters;
using OmniFlow.Application.Wrappers;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Infrastructure.Repositories;

public class PostRepositoryAsync : GenericRepositoryAsync<Post>, IPostRepositoryAsync
{
    public PostRepositoryAsync(IApplicationDbContext context) : base(context)
    {
    }

    public Task<Post?> GetByIdWithUserAsync(Guid postId)
    {
        return WithPostDetails().FirstOrDefaultAsync(post => post.Id == postId);
    }

    public Task<bool> CanLinkPublishedTripAsync(Guid tripId, Guid userId, CancellationToken cancellationToken)
    {
        return _context.Trips.AnyAsync(
            trip => trip.Id == tripId && trip.OwnerId == userId && trip.Status == TripStatus.Published,
            cancellationToken);
    }

    public Task<PagedResponse<Post>> GetByUserAsync(Guid userId, RequestParameter parameter)
    {
        return ToPageAsync(
            WithPostDetails().Where(post => post.UserId == userId).OrderByDescending(post => post.CreatedAt),
            parameter);
    }

    public Task<PagedResponse<Post>> GetVisibleByUserAsync(Guid userId, RequestParameter parameter)
    {
        return ToPageAsync(
            WithPostDetails()
                .Where(post => post.UserId == userId && post.DeletedAt == null && post.IsVisible)
                .OrderByDescending(post => post.CreatedAt),
            parameter);
    }

    public async Task<PagedResponse<Post>> GetLikedVisibleByUserAsync(
        Guid userId,
        RequestParameter parameter,
        IReadOnlyCollection<Guid>? excludedAuthorIds = null)
    {
        var query = WithPostDetails().Where(post =>
            post.DeletedAt == null &&
            post.IsVisible &&
            _context.PostUpvotes.Any(upvote => upvote.UserId == userId && upvote.PostId == post.Id));

        if (excludedAuthorIds is { Count: > 0 })
        {
            query = query.Where(post => !excludedAuthorIds.Contains(post.UserId));
        }

        var orderedQuery = query
            .OrderByDescending(post => _context.PostUpvotes
                .Where(upvote => upvote.UserId == userId && upvote.PostId == post.Id)
                .Select(upvote => upvote.CreatedAt)
                .FirstOrDefault())
            .ThenByDescending(post => post.Id);

        return await ToPageAsync(orderedQuery, parameter);
    }

    public Task<PagedResponse<Post>> GetVisibleAsync(RequestParameter parameter)
    {
        return ToPageAsync(
            WithPostDetails().Where(post => post.IsVisible).OrderByDescending(post => post.CreatedAt),
            parameter);
    }

    private IQueryable<Post> WithPostDetails()
    {
        return _dbSet
            .Include(post => post.User)
            .Include(post => post.Trip)
                .ThenInclude(trip => trip!.Destinations);
    }

    private static async Task<PagedResponse<Post>> ToPageAsync(
        IQueryable<Post> query,
        RequestParameter parameter)
    {
        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((parameter.PageNumber - 1) * parameter.PageSize)
            .Take(parameter.PageSize)
            .ToListAsync();

        return new PagedResponse<Post>(items, parameter.PageNumber, parameter.PageSize, totalCount);
    }
}
