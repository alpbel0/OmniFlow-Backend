using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.Features.Posts.Queries.GetFeed;

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
        [FromQuery] int pageSize = 20)
    {
        var query = new GetFeedQuery(new GetFeedParameter
        {
            Tab = tab,
            Cursor = cursor,
            PageSize = pageSize
        });

        var result = await Mediator.Send(query);
        return Ok(result);
    }
}