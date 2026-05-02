using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.DTOs.Posts;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Helpers;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Application.Parameters;
using OmniFlow.Application.Wrappers;

namespace OmniFlow.Application.Features.Posts.Queries.GetPostsByUser;

public class GetPostsByUserQueryHandler : IRequestHandler<GetPostsByUserQuery, PagedResponse<PostResponse>>
{
	private readonly IPostRepositoryAsync _postRepository;
	private readonly IApplicationDbContext _context;
	private readonly IAuthenticatedUserService _authenticatedUserService;
	private readonly IMapper _mapper;

	public GetPostsByUserQueryHandler(
		IPostRepositoryAsync postRepository,
		IApplicationDbContext context,
		IAuthenticatedUserService authenticatedUserService,
		IMapper mapper)
	{
		_postRepository = postRepository;
		_context = context;
		_authenticatedUserService = authenticatedUserService;
		_mapper = mapper;
	}

	public async Task<PagedResponse<PostResponse>> Handle(GetPostsByUserQuery request, CancellationToken cancellationToken)
	{
		var userExists = await _context.Users.AnyAsync(user => user.Id == request.UserId, cancellationToken);
		if (!userExists)
		{
			throw new EntityNotFoundException("User", request.UserId);
		}

		Guid? currentUserId = null;
		if (Guid.TryParse(_authenticatedUserService.UserId, out var parsedUserId))
		{
			currentUserId = parsedUserId;
		}

		if (currentUserId.HasValue && currentUserId.Value != request.UserId)
		{
			var hasBlockRelationship = await BlockVisibilityHelper.HasBlockRelationshipAsync(
				_context,
				currentUserId.Value,
				request.UserId,
				cancellationToken);

			if (hasBlockRelationship)
			{
				return new PagedResponse<PostResponse>(
					Array.Empty<PostResponse>(),
					request.PageNumber,
					request.PageSize,
					0);
			}
		}

		var postsPage = await _postRepository.GetVisibleByUserAsync(
			request.UserId,
			new RequestParameter
			{
				PageNumber = request.PageNumber,
				PageSize = request.PageSize
			});

		var responses = _mapper.Map<List<PostResponse>>(postsPage.Data);
		if (currentUserId.HasValue && responses.Count > 0)
		{
			var postIds = responses.Select(post => post.Id).ToList();
			var upvotedPostIds = (await _context.PostUpvotes
				.Where(upvote => upvote.UserId == currentUserId.Value && postIds.Contains(upvote.PostId))
				.Select(upvote => upvote.PostId)
				.ToListAsync(cancellationToken))
				.ToHashSet();

			foreach (var response in responses)
			{
				response.IsUpvoted = upvotedPostIds.Contains(response.Id);
			}
		}

		return new PagedResponse<PostResponse>(
			responses,
			postsPage.PageNumber,
			postsPage.PageSize,
			postsPage.TotalCount);
	}
}
