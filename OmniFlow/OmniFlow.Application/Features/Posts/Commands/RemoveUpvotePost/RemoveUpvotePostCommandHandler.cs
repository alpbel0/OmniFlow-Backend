using MediatR;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.Posts.Commands.RemoveUpvotePost;

/// <summary>
/// Handler for removing an upvote from a post.
/// Business rules:
/// - Post must exist
/// - User must have previously upvoted the post
/// - UpvoteCount decrements (clamped to >= 0)
/// - Karma is revoked for post owner
/// </summary>
public class RemoveUpvotePostCommandHandler : IRequestHandler<RemoveUpvotePostCommand, Unit>
{
	private readonly IPostRepositoryAsync _postRepository;
	private readonly IApplicationDbContext _context;
	private readonly IAuthenticatedUserService _authenticatedUserService;
	private readonly IKarmaService _karmaService;

	public RemoveUpvotePostCommandHandler(
		IPostRepositoryAsync postRepository,
		IApplicationDbContext context,
		IAuthenticatedUserService authenticatedUserService,
		IKarmaService karmaService)
	{
		_postRepository = postRepository;
		_context = context;
		_authenticatedUserService = authenticatedUserService;
		_karmaService = karmaService;
	}

	public async Task<Unit> Handle(RemoveUpvotePostCommand request, CancellationToken cancellationToken)
	{
		var post = await _postRepository.GetByIdWithUserAsync(request.PostId);
		if (post == null)
		{
			throw new EntityNotFoundException("Post", request.PostId);
		}

		var userId = Guid.Parse(_authenticatedUserService.UserId);

		var existingUpvote = await _context.PostUpvotes
			.FindAsync(new object[] { request.PostId, userId }, cancellationToken);

		if (existingUpvote == null)
		{
			throw new EntityNotFoundException("PostUpvote", request.PostId);
		}

		_context.PostUpvotes.Remove(existingUpvote);
		post.UpvoteCount = Math.Max(0, post.UpvoteCount - 1);
		await _context.SaveChangesAsync(cancellationToken);

		await _karmaService.RevokeKarmaAsync(
			post.UserId,
			userId,
			KarmaEventType.PostUpvoted,
			post.Id,
			KarmaSourceType.Post);

		return Unit.Value;
	}
}