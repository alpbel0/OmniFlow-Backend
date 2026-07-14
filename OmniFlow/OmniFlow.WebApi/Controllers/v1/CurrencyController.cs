using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.DTOs.Currency;
using OmniFlow.Application.Features.Currency.Queries.GetCurrencyRates;

namespace OmniFlow.WebApi.Controllers.v1;

[Route("api/v1/currency")]
public sealed class CurrencyController : BaseApiController
{
    [HttpGet("rates")]
    [ProducesResponseType(typeof(CurrencyRatesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetRates(
        [FromQuery(Name = "base")] string baseCurrencyCode,
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new GetCurrencyRatesQuery(baseCurrencyCode, date),
            cancellationToken);
        return Ok(result);
    }
}
