using MediatR;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;
using OmniFlow.Domain.Exceptions;

namespace OmniFlow.Application.Features.Posts.Commands.UpvotePost;

public class UpvotePostCommandHandler : IRequestHandler<UpvotePostCommand, Unit>
{
    private readonly IPostRepositoryAsync _postRepository;
    private readonly IApplicationDbContext _context;
    private readonly IAuthenticatedUserService _authenticatedUserService;
    private readonly IKarmaService _karmaService;
    private readonly INotificationService _notificationService;

    public UpvotePostCommandHandler(
        IPostRepositoryAsync postRepository,
        IApplicationDbContext context,
        IAuthenticatedUserService authenticatedUserService,
        IKarmaService karmaService,
        INotificationService notificationService)
    {
        _postRepository = postRepository;
        _context = context;
        _authenticatedUserService = authenticatedUserService;
        _karmaService = karmaService;
        _notificationService = notificationService;
    }

    public async Task<Unit> Handle(UpvotePostCommand request, CancellationToken cancellationToken)
    {
        var post = await _postRepository.GetByIdWithUserAsync(request.PostId);
        if (post == null)
        {
            throw new EntityNotFoundException("Post", request.PostId);
        }

        var userId = Guid.Parse(_authenticatedUserService.UserId);
        var existingUpvote = await _context.PostUpvotes.FindAsync(new object[] { request.PostId, userId }, cancellationToken);
        if (existingUpvote != null)
        {
            throw new DuplicateUpvoteException("Post", request.PostId);
        }

        await _context.PostUpvotes.AddAsync(new PostUpvote
        {
            PostId = request.PostId,
            UserId = userId
        }, cancellationToken);

        post.UpvoteCount += 1;
        await _context.SaveChangesAsync(cancellationToken);
        await _karmaService.AwardKarmaAsync(
            post.UserId,
            userId,
            OmniFlow.Domain.Enums.KarmaEventType.PostUpvoted,
            1,
            post.Id,
            OmniFlow.Domain.Enums.KarmaSourceType.Post);
        await _notificationService.CreateNotificationAsync(
            post.UserId,
            userId,
            NotificationType.PostUpvote,
            post.Id,
            NotificationTargetType.Post);

        return Unit.Value;
    }
}