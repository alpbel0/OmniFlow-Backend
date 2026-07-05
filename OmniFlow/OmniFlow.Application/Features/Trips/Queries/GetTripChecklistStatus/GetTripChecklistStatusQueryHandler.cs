using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.Trips.Checklist;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Application.Features.Trips.Queries.GetTripChecklistStatus;

public class GetTripChecklistStatusQueryHandler : IRequestHandler<GetTripChecklistStatusQuery, TripChecklistStatusResponse>
{
	private readonly IApplicationDbContext _context;
	private readonly IAuthenticatedUserService _authenticatedUserService;
	private readonly ITripVisibilityService _tripVisibilityService;

	public GetTripChecklistStatusQueryHandler(
		IApplicationDbContext context,
		IAuthenticatedUserService authenticatedUserService,
		ITripVisibilityService tripVisibilityService)
	{
		_context = context;
		_authenticatedUserService = authenticatedUserService;
		_tripVisibilityService = tripVisibilityService;
	}

	public async Task<TripChecklistStatusResponse> Handle(GetTripChecklistStatusQuery request, CancellationToken cancellationToken)
	{
		var trip = await _context.Trips
			.AsNoTracking()
			.FirstOrDefaultAsync(t => t.Id == request.TripId, cancellationToken);

		if (trip is null)
			throw new EntityNotFoundException("Trip", request.TripId);

		_tripVisibilityService.EnsureVisibleOrThrow(trip, _authenticatedUserService.UserId);

		var destinations = await _context.TripDestinations
			.AsNoTracking()
			.Where(d => d.TripId == request.TripId)
			.OrderBy(d => d.OrderIndex)
			.ThenBy(d => d.Id)
			.ToListAsync(cancellationToken);

		var validItemKeys = TripChecklistItemKeyGenerator.Generate(destinations);
		var validItemKeySet = validItemKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);

		var confirmations = await _context.TripChecklistConfirmations
			.AsNoTracking()
			.Where(c => c.TripId == request.TripId && validItemKeys.Contains(c.ItemKey))
			.ToDictionaryAsync(c => c.ItemKey, StringComparer.OrdinalIgnoreCase, cancellationToken);

		return new TripChecklistStatusResponse
		{
			Items = validItemKeys
				.Select(itemKey =>
				{
					confirmations.TryGetValue(itemKey, out var confirmation);
					return new TripChecklistItemResponse
					{
						ItemKey = itemKey,
						IsConfirmed = confirmation?.IsConfirmed ?? false,
						ConfirmedAt = confirmation?.ConfirmedAt
					};
				})
				.ToList()
		};
	}
}
