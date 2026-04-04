using AutoMapper;
using MediatR;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.Comments.Commands.CreateComment;

public class CreateCommentCommandHandler : IRequestHandler<CreateCommentCommand, Guid>
{
	private readonly ICommentRepositoryAsync _commentRepository;
	private readonly IApplicationDbContext _context;
	private readonly IAuthenticatedUserService _authenticatedUserService;
	private readonly IMapper _mapper;
	private readonly INotificationService _notificationService;

	public CreateCommentCommandHandler(
		ICommentRepositoryAsync commentRepository,
		IApplicationDbContext context,
		IAuthenticatedUserService authenticatedUserService,
		IMapper mapper,
		INotificationService notificationService)
	{
		_commentRepository = commentRepository;
		_context = context;
		_authenticatedUserService = authenticatedUserService;
		_mapper = mapper;
		_notificationService = notificationService;
	}

	public async Task<Guid> Handle(CreateCommentCommand request, CancellationToken cancellationToken)
	{
		var post = _context.Posts.FirstOrDefault(x => x.Id == request.PostId);
		if (post == null)
		{
			throw new EntityNotFoundException("Post", request.PostId);
		}

		if (request.ParentCommentId.HasValue)
		{
			var parentComment = await _commentRepository.GetByIdWithRepliesAsync(request.ParentCommentId.Value);
			if (parentComment == null || parentComment.PostId != request.PostId)
			{
				throw new EntityNotFoundException("Comment", request.ParentCommentId.Value);
			}
		}

		var comment = _mapper.Map<Comment>(request);
		comment.UserId = Guid.Parse(_authenticatedUserService.UserId);
		comment.PostId = request.PostId;
		comment.Mentions = request.Mentions ?? new List<string>();

		post.CommentCount += 1;
		await _commentRepository.AddAsync(comment);

		await _notificationService.CreateNotificationAsync(
			post.UserId,
			comment.UserId,
			NotificationType.Comment,
			post.Id,
			NotificationTargetType.Post);

		var mentionedUsernames = (request.Mentions ?? new List<string>())
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.Select(x => x.Trim().TrimStart('@'))
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();

		if (mentionedUsernames.Count > 0)
		{
			var mentionedUserIds = _context.Users
				.Where(user => mentionedUsernames.Contains(user.Username))
				.Select(user => user.Id)
				.ToList();

			foreach (var mentionedUserId in mentionedUserIds)
			{
				await _notificationService.CreateNotificationAsync(
					mentionedUserId,
					comment.UserId,
					NotificationType.Mention,
					post.Id,
					NotificationTargetType.Post);
			}
		}

		return comment.Id;
	}
}