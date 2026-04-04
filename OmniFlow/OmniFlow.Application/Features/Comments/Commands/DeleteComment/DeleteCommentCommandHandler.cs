using MediatR;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;

namespace OmniFlow.Application.Features.Comments.Commands.DeleteComment;

public class DeleteCommentCommandHandler : IRequestHandler<DeleteCommentCommand, Unit>
{
	private readonly ICommentRepositoryAsync _commentRepository;
	private readonly IAuthenticatedUserService _authenticatedUserService;

	public DeleteCommentCommandHandler(
		ICommentRepositoryAsync commentRepository,
		IAuthenticatedUserService authenticatedUserService)
	{
		_commentRepository = commentRepository;
		_authenticatedUserService = authenticatedUserService;
	}

	public async Task<Unit> Handle(DeleteCommentCommand request, CancellationToken cancellationToken)
	{
		var comment = await _commentRepository.GetByIdWithRepliesAsync(request.CommentId);
		if (comment == null)
		{
			throw new EntityNotFoundException("Comment", request.CommentId);
		}

		var currentUserId = Guid.Parse(_authenticatedUserService.UserId);
		if (comment.UserId != currentUserId)
		{
			throw new ForbiddenException("You are not authorized to delete this comment.");
		}

		if (comment.Post != null && comment.Post.CommentCount > 0)
		{
			comment.Post.CommentCount -= 1;
		}

		await _commentRepository.DeleteAsync(comment);
		return Unit.Value;
	}
}