using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.Trips.Checklist;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Application.Features.Trips.Commands.ToggleChecklistItem;

public class ToggleChecklistItemCommandHandler : IRequestHandler<ToggleChecklistItemCommand, Unit>
{
	private readonly IApplicationDbContext _context;
	private readonly IAuthenticatedUserService _authenticatedUserService;

	public ToggleChecklistItemCommandHandler(
		IApplicationDbContext context,
		IAuthenticatedUserService authenticatedUserService)
	{
		_context = context;
		_authenticatedUserService = authenticatedUserService;
	}

	public async Task<Unit> Handle(ToggleChecklistItemCommand request, CancellationToken cancellationToken)
	{
		var trip = await _context.Trips
			.FirstOrDefaultAsync(t => t.Id == request.TripId, cancellationToken);

		if (trip is null)
			throw new EntityNotFoundException("Trip", request.TripId);

		var currentUserId = Guid.Parse(_authenticatedUserService.UserId);
		if (trip.OwnerId != currentUserId)
			throw new ForbiddenException("You are not authorized to modify this trip.");

		var destinations = await _context.TripDestinations
			.AsNoTracking()
			.Where(d => d.TripId == request.TripId)
			.OrderBy(d => d.OrderIndex)
			.ThenBy(d => d.Id)
			.ToListAsync(cancellationToken);

		var validItemKey = TripChecklistItemKeyGenerator
			.Generate(destinations)
			.FirstOrDefault(itemKey => itemKey.Equals(request.ItemKey, StringComparison.OrdinalIgnoreCase));

		if (validItemKey is null)
			throw new EntityNotFoundException("TripChecklistConfirmation", request.ItemKey);

		var confirmation = await _context.TripChecklistConfirmations
			.FirstOrDefaultAsync(
				c => c.TripId == request.TripId && c.ItemKey == validItemKey,
				cancellationToken);

		if (confirmation is null)
		{
			confirmation = new TripChecklistConfirmation
			{
				TripId = request.TripId,
				ItemKey = validItemKey
			};

			await _context.TripChecklistConfirmations.AddAsync(confirmation, cancellationToken);
		}

		confirmation.IsConfirmed = request.IsConfirmed;
		confirmation.ConfirmedAt = request.IsConfirmed ? DateTime.UtcNow : null;

		await _context.SaveChangesAsync(cancellationToken);

		return Unit.Value;
	}
}
