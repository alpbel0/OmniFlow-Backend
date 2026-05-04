using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Helpers;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Application.Parameters;
using OmniFlow.Application.Wrappers;

namespace OmniFlow.Application.Features.Trips.Queries.GetPublishedTripsByUser;

public class GetPublishedTripsByUserQueryHandler : IRequestHandler<GetPublishedTripsByUserQuery, PagedResponse<TripResponse>>
{
	private readonly ITripRepositoryAsync _tripRepository;
	private readonly IApplicationDbContext _context;
	private readonly IAuthenticatedUserService _authenticatedUserService;
	private readonly IMapper _mapper;

	public GetPublishedTripsByUserQueryHandler(
		ITripRepositoryAsync tripRepository,
		IApplicationDbContext context,
		IAuthenticatedUserService authenticatedUserService,
		IMapper mapper)
	{
		_tripRepository = tripRepository;
		_context = context;
		_authenticatedUserService = authenticatedUserService;
		_mapper = mapper;
	}

	public async Task<PagedResponse<TripResponse>> Handle(GetPublishedTripsByUserQuery request, CancellationToken cancellationToken)
	{
		var userExists = await _context.Users.AnyAsync(user => user.Id == request.UserId, cancellationToken);
		if (!userExists)
		{
			throw new EntityNotFoundException("User", request.UserId);
		}

		Guid? currentUserId = null;
		if (Guid.TryParse(_authenticatedUserService.UserId, out var parsedUserId))
		{
			currentUserId = parsedUserId;
		}

		if (currentUserId.HasValue && currentUserId.Value != request.UserId)
		{
			var hasBlockRelationship = await BlockVisibilityHelper.HasBlockRelationshipAsync(
				_context,
				currentUserId.Value,
				request.UserId,
				cancellationToken);

			if (hasBlockRelationship)
			{
				return new PagedResponse<TripResponse>(
					Array.Empty<TripResponse>(),
					request.PageNumber,
					request.PageSize,
					0);
			}
		}

		var tripsPage = await _tripRepository.GetPublishedByOwnerAsync(
			request.UserId,
			new RequestParameter
			{
				PageNumber = request.PageNumber,
				PageSize = request.PageSize
			});

		var responses = _mapper.Map<List<TripResponse>>(tripsPage.Data);
		return new PagedResponse<TripResponse>(
			responses,
			tripsPage.PageNumber,
			tripsPage.PageSize,
			tripsPage.TotalCount);
	}
}