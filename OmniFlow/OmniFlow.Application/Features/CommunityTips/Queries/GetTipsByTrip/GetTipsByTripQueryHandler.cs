using AutoMapper;
using MediatR;
using OmniFlow.Application.DTOs.CommunityTips;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Application.Parameters;
using OmniFlow.Application.Wrappers;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Application.Features.CommunityTips.Queries.GetTipsByTrip;

public class GetTipsByTripQueryHandler : IRequestHandler<GetTipsByTripQuery, PagedResponse<TipResponse>>
{
	private readonly ICommunityTipRepositoryAsync _tipRepository;
	private readonly IApplicationDbContext _context;
	private readonly IAuthenticatedUserService _authenticatedUserService;
	private readonly IMapper _mapper;

	public GetTipsByTripQueryHandler(
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

	public async Task<PagedResponse<TipResponse>> Handle(GetTipsByTripQuery request, CancellationToken cancellationToken)
	{
		var tripExists = _context.Trips.Any(x => x.Id == request.TripId);
		if (!tripExists)
		{
			throw new EntityNotFoundException("Trip", request.TripId);
		}

		var parameter = new RequestParameter
		{
			PageNumber = request.PageNumber,
			PageSize = request.PageSize
		};

		var tips = await _tipRepository.GetByTripAsync(request.TripId, parameter);
		var currentUserId = Guid.Parse(_authenticatedUserService.UserId);

		var mappedTips = tips.Data
			.Select(tip => MapTip(tip, currentUserId))
			.ToList();

		return new PagedResponse<TipResponse>(mappedTips, tips.PageNumber, tips.PageSize, tips.TotalCount);
	}

	private TipResponse MapTip(CommunityTip tip, Guid currentUserId)
	{
		var response = _mapper.Map<TipResponse>(tip);
		response.IsUpvoted = _context.TipUpvotes.Any(x => x.TipId == tip.Id && x.UserId == currentUserId);
		return response;
	}
}