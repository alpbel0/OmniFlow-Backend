using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.DTOs.Karma;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Wrappers;

namespace OmniFlow.Application.Features.Karma.Queries.GetKarmaHistory;

public class GetKarmaHistoryQueryHandler : IRequestHandler<GetKarmaHistoryQuery, PagedResponse<KarmaEventResponse>>
{
	private readonly IApplicationDbContext _context;
	private readonly IAuthenticatedUserService _authenticatedUserService;

	public GetKarmaHistoryQueryHandler(
		IApplicationDbContext context,
		IAuthenticatedUserService authenticatedUserService)
	{
		_context = context;
		_authenticatedUserService = authenticatedUserService;
	}

	public async Task<PagedResponse<KarmaEventResponse>> Handle(GetKarmaHistoryQuery request, CancellationToken cancellationToken)
	{
		if (!Guid.TryParse(_authenticatedUserService.UserId, out var currentUserId))
		{
			return new PagedResponse<KarmaEventResponse>(new List<KarmaEventResponse>(), 1, 10, 0);
		}

		var pageNumber = request.PageNumber > 0 ? request.PageNumber : 1;
		var pageSize = request.PageSize > 0 ? request.PageSize : 10;

		var query = _context.KarmaEvents
			.AsNoTracking()
			.Include(x => x.Actor)
			.Where(x => x.UserId == currentUserId)
			.OrderByDescending(x => x.CreatedAt);

		var totalCount = await query.CountAsync(cancellationToken);
		var events = await query
			.Skip((pageNumber - 1) * pageSize)
			.Take(pageSize)
			.Select(x => new KarmaEventResponse
			{
				EventType = x.EventType,
				Points = x.Points,
				SourceType = x.SourceType,
				CreatedAt = x.CreatedAt,
				ActorUsername = x.Actor != null ? x.Actor.Username : null
			})
			.ToListAsync(cancellationToken);

		return new PagedResponse<KarmaEventResponse>(events, pageNumber, pageSize, totalCount);
	}
}
