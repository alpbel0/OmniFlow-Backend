using MediatR;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Application.Features.Comments.Commands.RemoveUpvoteComment;

/// <summary>
/// Handler for removing an upvote from a comment.
/// Business rules:
/// - Comment must exist
/// - User must have previously upvoted the comment
/// - UpvoteCount decrements (clamped to >= 0)
/// </summary>
public class RemoveUpvoteCommentCommandHandler : IRequestHandler<RemoveUpvoteCommentCommand, Unit>
{
	private readonly ICommentRepositoryAsync _commentRepository;
	private readonly IApplicationDbContext _context;
	private readonly IAuthenticatedUserService _authenticatedUserService;

	public RemoveUpvoteCommentCommandHandler(
		ICommentRepositoryAsync commentRepository,
		IApplicationDbContext context,
		IAuthenticatedUserService authenticatedUserService)
	{
		_commentRepository = commentRepository;
		_context = context;
		_authenticatedUserService = authenticatedUserService;
	}

	public async Task<Unit> Handle(RemoveUpvoteCommentCommand request, CancellationToken cancellationToken)
	{
		var comment = await _commentRepository.GetByIdWithRepliesAsync(request.CommentId);
		if (comment == null)
		{
			throw new EntityNotFoundException("Comment", request.CommentId);
		}

		var userId = Guid.Parse(_authenticatedUserService.UserId);

		var existingUpvote = await _context.CommentUpvotes
			.FindAsync(new object[] { request.CommentId, userId }, cancellationToken);

		if (existingUpvote == null)
		{
			throw new EntityNotFoundException("CommentUpvote", request.CommentId);
		}

		_context.CommentUpvotes.Remove(existingUpvote);
		comment.UpvoteCount = Math.Max(0, comment.UpvoteCount - 1);
		await _context.SaveChangesAsync(cancellationToken);

		return Unit.Value;
	}
}