using AutoMapper;
using MediatR;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Application.Features.Posts;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Application.Features.Posts.Commands.CreatePost;

public class CreatePostCommandHandler : IRequestHandler<CreatePostCommand, Guid>
{
    private readonly IPostRepositoryAsync _postRepository;
    private readonly IAuthenticatedUserService _authenticatedUserService;
    private readonly IMapper _mapper;

    public CreatePostCommandHandler(
        IPostRepositoryAsync postRepository,
        IAuthenticatedUserService authenticatedUserService,
        IMapper mapper)
    {
        _postRepository = postRepository;
        _authenticatedUserService = authenticatedUserService;
        _mapper = mapper;
    }

    public async Task<Guid> Handle(CreatePostCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = Guid.Parse(_authenticatedUserService.UserId);

        await PostCommandGuard.EnsureTripCanBeLinkedAsync(
            _postRepository,
            request.PostType,
            request.TripId,
            currentUserId,
            cancellationToken);

        var post = _mapper.Map<Post>(request);
        post.UserId = currentUserId;

        await _postRepository.AddAsync(post);
        return post.Id;
    }
}
