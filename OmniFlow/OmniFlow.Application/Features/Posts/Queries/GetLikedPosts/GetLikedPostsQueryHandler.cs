using AutoMapper;
using MediatR;
using OmniFlow.Application.DTOs.Posts;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Helpers;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Application.Parameters;
using OmniFlow.Application.Wrappers;

namespace OmniFlow.Application.Features.Posts.Queries.GetLikedPosts;

public class GetLikedPostsQueryHandler : IRequestHandler<GetLikedPostsQuery, PagedResponse<PostResponse>>
{
	private readonly IPostRepositoryAsync _postRepository;
	private readonly IApplicationDbContext _context;
	private readonly IAuthenticatedUserService _authenticatedUserService;
	private readonly IMapper _mapper;

	public GetLikedPostsQueryHandler(
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

	public async Task<PagedResponse<PostResponse>> Handle(GetLikedPostsQuery request, CancellationToken cancellationToken)
	{
		if (!Guid.TryParse(_authenticatedUserService.UserId, out var currentUserId))
		{
			throw new ForbiddenException("Authenticated user could not be resolved.");
		}

		var postsPage = await _postRepository.GetLikedVisibleByUserAsync(
			currentUserId,
			new RequestParameter
			{
				PageNumber = request.PageNumber,
				PageSize = request.PageSize
			},
			await BlockVisibilityHelper.GetBlockedUserIdsAsync(
				_context,
				currentUserId,
				cancellationToken));

		var responses = _mapper.Map<List<PostResponse>>(postsPage.Data);
		foreach (var response in responses)
		{
			response.IsUpvoted = true;
		}

		return new PagedResponse<PostResponse>(
			responses,
			postsPage.PageNumber,
			postsPage.PageSize,
			postsPage.TotalCount);
	}
}
