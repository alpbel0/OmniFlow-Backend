using MediatR;
using OmniFlow.Application.DTOs.Posts;
using OmniFlow.Application.Wrappers;

namespace OmniFlow.Application.Features.Posts.Queries.GetMyPosts;

public class GetMyPostsQuery : IRequest<PagedResponse<PostResponse>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
