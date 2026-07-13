using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.Features.Posts.Queries.GetFeed;
using OmniFlow.Domain.Enums;

namespace OmniFlow.WebApi.Controllers.v1;

/// <summary>
/// Feed endpoint with tab-based cursor pagination.
/// Authentication is required by the base controller.
/// </summary>
public class FeedController : BaseApiController
{
    /// <summary>Get the personalized or latest feed.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(GetFeedViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Get(
        [FromQuery] FeedTab tab = FeedTab.Latest,
        [FromQuery] string? cursor = null,
        [FromQuery] int pageSize = 20,
        [FromQuery(Name = "q")] string? searchQuery = null,
        [FromQuery] string? tag = null,
        [FromQuery] PostType? postType = null,
        [FromQuery] FeedSort sort = FeedSort.Latest)
    {
        var feedQuery = new GetFeedQuery(new GetFeedParameter
        {
            Tab = tab,
            Cursor = cursor,
            PageSize = pageSize,
            Query = searchQuery,
            Tag = tag,
            PostType = postType,
            Sort = sort
        });

        var result = await Mediator.Send(feedQuery);
        return Ok(result);
    }
}
