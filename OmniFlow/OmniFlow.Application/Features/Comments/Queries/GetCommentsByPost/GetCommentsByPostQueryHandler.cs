using AutoMapper;
using MediatR;
using OmniFlow.Application.DTOs.Comments;
using OmniFlow.Application.Exceptions;
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
		var postExists = _context.Posts.Any(x => x.Id == request.PostId);
		if (!postExists)
		{
			throw new EntityNotFoundException("Post", request.PostId);
		}

		var parameter = new RequestParameter
		{
			PageNumber = request.PageNumber,
			PageSize = request.PageSize
		};

		var comments = await _commentRepository.GetByPostAsync(request.PostId, parameter);
		var currentUserId = Guid.Parse(_authenticatedUserService.UserId);

		var mappedComments = comments.Data
			.Select(comment => MapCommentTree(comment, currentUserId))
			.ToList();

		return new PagedResponse<CommentResponse>(mappedComments, comments.PageNumber, comments.PageSize, comments.TotalCount);
	}

	private CommentResponse MapCommentTree(Comment comment, Guid currentUserId)
	{
		var response = _mapper.Map<CommentResponse>(comment);
		response.IsUpvoted = _context.CommentUpvotes.Any(x => x.CommentId == comment.Id && x.UserId == currentUserId);
		response.Replies = comment.Replies
			.OrderBy(reply => reply.CreatedAt)
			.Select(reply => MapCommentTree(reply, currentUserId))
			.ToList();
		return response;
	}
}