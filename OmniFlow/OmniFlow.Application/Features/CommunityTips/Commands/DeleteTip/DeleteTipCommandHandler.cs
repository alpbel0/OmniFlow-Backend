using AutoMapper;
using MediatR;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;

namespace OmniFlow.Application.Features.CommunityTips.Commands.DeleteTip;

public class DeleteTipCommandHandler : IRequestHandler<DeleteTipCommand, Unit>
{
	private readonly ICommunityTipRepositoryAsync _tipRepository;
	private readonly IAuthenticatedUserService _authenticatedUserService;
	private readonly IMapper _mapper;

	public DeleteTipCommandHandler(
		ICommunityTipRepositoryAsync tipRepository,
		IAuthenticatedUserService authenticatedUserService,
		IMapper mapper)
	{
		_tipRepository = tipRepository;
		_authenticatedUserService = authenticatedUserService;
		_mapper = mapper;
	}

	public async Task<Unit> Handle(DeleteTipCommand request, CancellationToken cancellationToken)
	{
		var tip = await _tipRepository.GetByIdAsync(request.TipId);
		if (tip == null)
		{
			throw new EntityNotFoundException("Tip", request.TipId);
		}

		var currentUserId = Guid.Parse(_authenticatedUserService.UserId);
		if (tip.UserId != currentUserId)
		{
			throw new ForbiddenException("You are not authorized to delete this tip.");
		}

		await _tipRepository.DeleteAsync(tip);
		return Unit.Value;
	}
}