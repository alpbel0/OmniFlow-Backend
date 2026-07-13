using AutoMapper;
using MediatR;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Application.Features.Posts;

namespace OmniFlow.Application.Features.Posts.Commands.UpdatePost;

public class UpdatePostCommandHandler : IRequestHandler<UpdatePostCommand, Unit>
{
    private readonly IPostRepositoryAsync _postRepository;
    private readonly IAuthenticatedUserService _authenticatedUserService;
    private readonly IMapper _mapper;

    public UpdatePostCommandHandler(
        IPostRepositoryAsync postRepository,
        IAuthenticatedUserService authenticatedUserService,
        IMapper mapper)
    {
        _postRepository = postRepository;
        _authenticatedUserService = authenticatedUserService;
        _mapper = mapper;
    }

    public async Task<Unit> Handle(UpdatePostCommand request, CancellationToken cancellationToken)
    {
        var post = await _postRepository.GetByIdWithUserAsync(request.PostId);
        if (post == null)
        {
            throw new EntityNotFoundException("Post", request.PostId);
        }

        var currentUserId = Guid.Parse(_authenticatedUserService.UserId);
        if (post.UserId != currentUserId)
        {
            throw new ForbiddenException("You are not authorized to update this post.");
        }

        _mapper.Map(request, post);

        PostCommandGuard.EnsureValidContent(post);
        await PostCommandGuard.EnsureTripCanBeLinkedAsync(
            _postRepository,
            post.PostType,
            post.TripId,
            currentUserId,
            cancellationToken);

        await _postRepository.UpdateAsync(post);

        return Unit.Value;
    }
}
