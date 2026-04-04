using MediatR;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;

namespace OmniFlow.Application.Features.Posts.Commands.DeletePost;

public class DeletePostCommandHandler : IRequestHandler<DeletePostCommand, Unit>
{
    private readonly IPostRepositoryAsync _postRepository;
    private readonly IAuthenticatedUserService _authenticatedUserService;

    public DeletePostCommandHandler(
        IPostRepositoryAsync postRepository,
        IAuthenticatedUserService authenticatedUserService)
    {
        _postRepository = postRepository;
        _authenticatedUserService = authenticatedUserService;
    }

    public async Task<Unit> Handle(DeletePostCommand request, CancellationToken cancellationToken)
    {
        var post = await _postRepository.GetByIdWithUserAsync(request.PostId);
        if (post == null)
        {
            throw new EntityNotFoundException("Post", request.PostId);
        }

        var currentUserId = Guid.Parse(_authenticatedUserService.UserId);
        if (post.UserId != currentUserId)
        {
            throw new ForbiddenException("You are not authorized to delete this post.");
        }

        await _postRepository.DeleteAsync(post);
        return Unit.Value;
    }
}