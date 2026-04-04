using AutoMapper;
using MediatR;
using OmniFlow.Application.DTOs.Posts;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;

namespace OmniFlow.Application.Features.Posts.Queries.GetPostById;

public class GetPostByIdQueryHandler : IRequestHandler<GetPostByIdQuery, PostResponse>
{
    private readonly IPostRepositoryAsync _postRepository;
    private readonly IApplicationDbContext _context;
    private readonly IAuthenticatedUserService _authenticatedUserService;
    private readonly IMapper _mapper;

    public GetPostByIdQueryHandler(
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

    public async Task<PostResponse> Handle(GetPostByIdQuery request, CancellationToken cancellationToken)
    {
        var post = await _postRepository.GetByIdWithUserAsync(request.PostId);
        if (post == null)
        {
            throw new EntityNotFoundException("Post", request.PostId);
        }

        var response = _mapper.Map<PostResponse>(post);

        var currentUserId = Guid.Parse(_authenticatedUserService.UserId);
        response.IsUpvoted = _context.PostUpvotes
            .Any(x => x.PostId == post.Id && x.UserId == currentUserId);

        return response;
    }
}
