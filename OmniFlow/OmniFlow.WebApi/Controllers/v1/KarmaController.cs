using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.DTOs.Karma;
using OmniFlow.Application.Features.Karma.Queries.GetKarmaHistory;
using OmniFlow.Application.Wrappers;

namespace OmniFlow.WebApi.Controllers.v1;

public class KarmaController : BaseApiController
{
	[HttpGet("history")]
	[ProducesResponseType(typeof(PagedResponse<KarmaEventResponse>), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public async Task<IActionResult> GetHistory([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
	{
		var result = await Mediator.Send(new GetKarmaHistoryQuery
		{
			PageNumber = pageNumber,
			PageSize = pageSize
		});

		return Ok(result);
	}
}
