using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.DTOs.Comments;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Helpers;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Application.Parameters;
using OmniFlow.Application.Wrappers;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Application.Features.Comments.Queries.GetCommentsByPost;

public class GetCommentsByPostQueryHandler : IRequestHandler<GetCommentsByPostQuery, PagedResponse<CommentResponse>>
{
	private readonly ICommentRepositoryAsync _commentRepository;
	private readonly IApplicationDbContext _context;
	private readonly IAuthenticatedUserService _authenticatedUserService;
	private readonly IMapper _mapper;

	public GetCommentsByPostQueryHandler(
		ICommentRepositoryAsync commentRepository,
		IApplicationDbContext context,
		IAuthenticatedUserService authenticatedUserService,
		IMapper mapper)
	{
		_commentRepository = commentRepository;
		_context = context;
		_authenticatedUserService = authenticatedUserService;
		_mapper = mapper;
	}

	public async Task<PagedResponse<CommentResponse>> Handle(GetCommentsByPostQuery request, CancellationToken cancellationToken)
	{
		var postOwner = await _context.Posts
			.Where(post => post.Id == request.PostId)
			.Select(post => new { post.Id, post.UserId })
			.FirstOrDefaultAsync(cancellationToken);

		if (postOwner == null)
		{
			throw new EntityNotFoundException("Post", request.PostId);
		}

		var currentUserId = Guid.Parse(_authenticatedUserService.UserId);
		var hasBlockRelationship = await BlockVisibilityHelper.HasBlockRelationshipAsync(
			_context,
			currentUserId,
			postOwner.UserId,
			cancellationToken);

		if (hasBlockRelationship)
		{
			throw new EntityNotFoundException("Post", request.PostId);
		}

		var blockedUserIds = await BlockVisibilityHelper.GetBlockedUserIdsAsync(_context, currentUserId, cancellationToken);

		var parameter = new RequestParameter
		{
			PageNumber = request.PageNumber,
			PageSize = request.PageSize
		};

		var comments = await _commentRepository.GetByPostAsync(request.PostId, parameter, blockedUserIds.ToList(), cancellationToken);

		var mappedComments = comments.Data
			.Select(comment => MapCommentTree(comment, currentUserId, blockedUserIds))
			.Where(comment => comment != null)
			.Select(comment => comment!)
			.ToList();

		return new PagedResponse<CommentResponse>(mappedComments, comments.PageNumber, comments.PageSize, comments.TotalCount);
	}

	private CommentResponse? MapCommentTree(Comment comment, Guid currentUserId, IReadOnlySet<Guid> blockedUserIds)
	{
		if (blockedUserIds.Contains(comment.UserId))
		{
			return null;
		}

		var response = _mapper.Map<CommentResponse>(comment);
		response.IsUpvoted = _context.CommentUpvotes.Any(x => x.CommentId == comment.Id && x.UserId == currentUserId);
		response.Replies = comment.Replies
			.OrderBy(reply => reply.CreatedAt)
			.Select(reply => MapCommentTree(reply, currentUserId, blockedUserIds))
			.Where(reply => reply != null)
			.Select(reply => reply!)
			.ToList();
		return response;
	}
}