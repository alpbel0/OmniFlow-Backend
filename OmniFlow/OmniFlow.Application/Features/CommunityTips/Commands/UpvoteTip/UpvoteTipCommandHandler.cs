using AutoMapper;
using MediatR;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;
using OmniFlow.Domain.Exceptions;

namespace OmniFlow.Application.Features.CommunityTips.Commands.UpvoteTip;

public class UpvoteTipCommandHandler : IRequestHandler<UpvoteTipCommand, Unit>
{
	private readonly ICommunityTipRepositoryAsync _tipRepository;
	private readonly IApplicationDbContext _context;
	private readonly IAuthenticatedUserService _authenticatedUserService;
	private readonly IMapper _mapper;
	private readonly IKarmaService _karmaService;
	private readonly INotificationService _notificationService;

	public UpvoteTipCommandHandler(
		ICommunityTipRepositoryAsync tipRepository,
		IApplicationDbContext context,
		IAuthenticatedUserService authenticatedUserService,
		IMapper mapper,
		IKarmaService karmaService,
		INotificationService notificationService)
	{
		_tipRepository = tipRepository;
		_context = context;
		_authenticatedUserService = authenticatedUserService;
		_mapper = mapper;
		_karmaService = karmaService;
		_notificationService = notificationService;
	}

	public async Task<Unit> Handle(UpvoteTipCommand request, CancellationToken cancellationToken)
	{
		var tip = await _tipRepository.GetByIdAsync(request.TipId);
		if (tip == null)
		{
			throw new EntityNotFoundException("Tip", request.TipId);
		}

		var userId = Guid.Parse(_authenticatedUserService.UserId);
		var existingUpvote = _context.TipUpvotes.Any(x => x.TipId == request.TipId && x.UserId == userId);
		if (existingUpvote)
		{
			throw new DuplicateUpvoteException("Tip", request.TipId);
		}

		await _context.TipUpvotes.AddAsync(new TipUpvote
		{
			TipId = request.TipId,
			UserId = userId
		}, cancellationToken);

		tip.UpvoteCount += 1;
		await _context.SaveChangesAsync(cancellationToken);
		await _karmaService.AwardKarmaAsync(
			tip.UserId,
			userId,
			OmniFlow.Domain.Enums.KarmaEventType.TipUpvoted,
			2,
			tip.Id,
			OmniFlow.Domain.Enums.KarmaSourceType.Tip);
		await _notificationService.CreateNotificationAsync(
			tip.UserId,
			userId,
			NotificationType.TipUpvote,
			tip.Id,
			NotificationTargetType.Tip);

		return Unit.Value;
	}
}