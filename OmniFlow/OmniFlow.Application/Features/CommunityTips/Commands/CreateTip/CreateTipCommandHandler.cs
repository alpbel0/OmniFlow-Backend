using AutoMapper;
using MediatR;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Application.Features.CommunityTips.Commands.CreateTip;

public class CreateTipCommandHandler : IRequestHandler<CreateTipCommand, Guid>
{
	private readonly ICommunityTipRepositoryAsync _tipRepository;
	private readonly IApplicationDbContext _context;
	private readonly IAuthenticatedUserService _authenticatedUserService;
	private readonly IMapper _mapper;

	public CreateTipCommandHandler(
		ICommunityTipRepositoryAsync tipRepository,
		IApplicationDbContext context,
		IAuthenticatedUserService authenticatedUserService,
		IMapper mapper)
	{
		_tipRepository = tipRepository;
		_context = context;
		_authenticatedUserService = authenticatedUserService;
		_mapper = mapper;
	}

	public async Task<Guid> Handle(CreateTipCommand request, CancellationToken cancellationToken)
	{
		var tripExists = _context.Trips.Any(x => x.Id == request.TripId);
		if (!tripExists)
		{
			throw new EntityNotFoundException("Trip", request.TripId);
		}

		if (request.PlaceId.HasValue)
		{
			var placeExists = _context.Places.Any(x => x.Id == request.PlaceId.Value);
			if (!placeExists)
			{
				throw new EntityNotFoundException("Place", request.PlaceId.Value);
			}
		}

		var tip = _mapper.Map<CommunityTip>(request);
		tip.Content = request.Content.Trim();
		tip.UserId = Guid.Parse(_authenticatedUserService.UserId);
		tip.IsVisible = true;

		await _tipRepository.AddAsync(tip);
		return tip.Id;
	}
}