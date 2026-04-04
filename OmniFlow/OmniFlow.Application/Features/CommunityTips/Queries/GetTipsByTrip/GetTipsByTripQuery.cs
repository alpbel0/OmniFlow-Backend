using MediatR;
using OmniFlow.Application.DTOs.CommunityTips;
using OmniFlow.Application.Wrappers;

namespace OmniFlow.Application.Features.CommunityTips.Queries.GetTipsByTrip;

public class GetTipsByTripQuery : IRequest<PagedResponse<TipResponse>>
{
	public Guid TripId { get; set; }

	public int PageNumber { get; set; } = 1;

	public int PageSize { get; set; } = 10;
}
