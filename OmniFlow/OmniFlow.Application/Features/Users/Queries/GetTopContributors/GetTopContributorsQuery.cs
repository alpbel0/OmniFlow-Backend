using MediatR;
using OmniFlow.Application.DTOs.Users;

namespace OmniFlow.Application.Features.Users.Queries.GetTopContributors;

public class GetTopContributorsQuery : IRequest<IReadOnlyList<TopContributorResponse>>
{
	public int Limit { get; set; } = 10;
}
