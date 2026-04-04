using MediatR;
using OmniFlow.Application.DTOs.Karma;
using OmniFlow.Application.Wrappers;

namespace OmniFlow.Application.Features.Karma.Queries.GetKarmaHistory;

public class GetKarmaHistoryQuery : IRequest<PagedResponse<KarmaEventResponse>>
{
	public int PageNumber { get; set; } = 1;

	public int PageSize { get; set; } = 10;
}
