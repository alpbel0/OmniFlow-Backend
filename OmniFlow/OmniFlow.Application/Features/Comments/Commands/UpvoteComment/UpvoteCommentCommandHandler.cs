using MediatR;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;
using OmniFlow.Domain.Exceptions;

namespace OmniFlow.Application.Features.Comments.Commands.UpvoteComment;

public class UpvoteCommentCommandHandler : IRequestHandler<UpvoteCommentCommand, Unit>
{
	private readonly ICommentRepositoryAsync _commentRepository;
	private readonly IApplicationDbContext _context;
	private readonly IAuthenticatedUserService _authenticatedUserService;
	private readonly INotificationService _notificationService;

	public UpvoteCommentCommandHandler(
		ICommentRepositoryAsync commentRepository,
		IApplicationDbContext context,
		IAuthenticatedUserService authenticatedUserService,
		INotificationService notificationService)
	{
		_commentRepository = commentRepository;
		_context = context;
		_authenticatedUserService = authenticatedUserService;
		_notificationService = notificationService;
	}

	public async Task<Unit> Handle(UpvoteCommentCommand request, CancellationToken cancellationToken)
	{
		var comment = await _commentRepository.GetByIdWithRepliesAsync(request.CommentId);
		if (comment == null)
		{
			throw new EntityNotFoundException("Comment", request.CommentId);
		}

		var userId = Guid.Parse(_authenticatedUserService.UserId);
		var existingUpvote = await _context.CommentUpvotes.FindAsync(new object[] { request.CommentId, userId }, cancellationToken);
		if (existingUpvote != null)
		{
			throw new DuplicateUpvoteException("Comment", request.CommentId);
		}

		await _context.CommentUpvotes.AddAsync(new CommentUpvote
		{
			CommentId = request.CommentId,
			UserId = userId
		}, cancellationToken);

		comment.UpvoteCount += 1;
		await _context.SaveChangesAsync(cancellationToken);
		await _notificationService.CreateNotificationAsync(
			comment.UserId,
			userId,
			NotificationType.CommentUpvote,
			comment.Id,
			NotificationTargetType.Comment);

		return Unit.Value;
	}
}