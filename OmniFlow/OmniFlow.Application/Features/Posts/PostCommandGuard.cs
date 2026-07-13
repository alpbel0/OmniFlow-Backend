using FluentValidation.Results;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.Posts;

internal static class PostCommandGuard
{
    public static async Task EnsureTripCanBeLinkedAsync(
        IPostRepositoryAsync repository,
        PostType postType,
        Guid? tripId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (postType != PostType.Route)
        {
            return;
        }

        var canLinkTrip = tripId.HasValue &&
            await repository.CanLinkPublishedTripAsync(tripId.Value, userId, cancellationToken);
        if (!canLinkTrip)
        {
            ThrowValidation(nameof(Post.TripId), "Route posts can only link one of your published trips.");
        }
    }

    public static void EnsureValidContent(Post post)
    {
        if (post.Photos.Count > 5)
        {
            ThrowValidation(nameof(Post.Photos), "A post can contain at most 5 photos.");
        }

        if (string.IsNullOrWhiteSpace(post.Content) && post.Photos.Count == 0)
        {
            ThrowValidation(nameof(Post.Content), "Post must contain content or at least one photo.");
        }
    }

    private static void ThrowValidation(string propertyName, string message)
    {
        throw new ValidationException(new[] { new ValidationFailure(propertyName, message) });
    }
}
