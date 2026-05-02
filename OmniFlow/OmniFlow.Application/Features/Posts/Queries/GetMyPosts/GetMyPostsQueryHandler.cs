using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.DTOs.Posts;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Application.Parameters;
using OmniFlow.Application.Wrappers;

namespace OmniFlow.Application.Features.Posts.Queries.GetMyPosts;

public class GetMyPostsQueryHandler : IRequestHandler<GetMyPostsQuery, PagedResponse<PostResponse>>
{
    private readonly IPostRepositoryAsync _postRepository;
    private readonly IApplicationDbContext _context;
    private readonly IAuthenticatedUserService _authenticatedUserService;
    private readonly IMapper _mapper;

    public GetMyPostsQueryHandler(
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

    public async Task<PagedResponse<PostResponse>> Handle(GetMyPostsQuery request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(_authenticatedUserService.UserId, out var currentUserId))
        {
            throw new ForbiddenException("Authenticated user could not be resolved.");
        }

        var postsPage = await _postRepository.GetByUserAsync(
            currentUserId,
            new RequestParameter
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            });

        var responses = _mapper.Map<List<PostResponse>>(postsPage.Data);
        var postIds = responses.Select(post => post.Id).ToList();

        var upvotedPostIds = postIds.Count == 0
            ? new HashSet<Guid>()
            : (await _context.PostUpvotes
                .Where(upvote => upvote.UserId == currentUserId && postIds.Contains(upvote.PostId))
                .Select(upvote => upvote.PostId)
                .ToListAsync(cancellationToken))
                .ToHashSet();

        foreach (var response in responses)
        {
            response.IsUpvoted = upvotedPostIds.Contains(response.Id);
        }

        return new PagedResponse<PostResponse>(
            responses,
            postsPage.PageNumber,
            postsPage.PageSize,
            postsPage.TotalCount);
    }
}
