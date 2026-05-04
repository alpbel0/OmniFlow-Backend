using MediatR;
using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.Wrappers;

namespace OmniFlow.Application.Features.Trips.Queries.GetPublishedTripsByUser;

public class GetPublishedTripsByUserQuery : IRequest<PagedResponse<TripResponse>>
{
	public Guid UserId { get; set; }
	public int PageNumber { get; set; } = 1;
	public int PageSize { get; set; } = 20;
}