using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.DTOs.Geocoding;
using OmniFlow.Application.Features.Geo.Queries.SearchCities;

namespace OmniFlow.WebApi.Controllers.v1;

/// <summary>
/// Geocoding API endpoints — city search for autocomplete (trip wizard, add destination).
/// Public, no authentication required.
/// </summary>
[ApiController]
[Route("api/v1/geo")]
[AllowAnonymous]
public class GeoController : ControllerBase
{
    private ISender? _mediator;

    protected ISender Mediator =>
        _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    /// <summary>Free-text city search (e.g. "Rom" -> Rome, Italy), backed by OpenStreetMap Nominatim.</summary>
    [HttpGet("cities")]
    [ProducesResponseType(typeof(IReadOnlyList<GeocodingResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchCities([FromQuery] string query, [FromQuery] int limit = 8)
    {
        var result = await Mediator.Send(new SearchCitiesQuery { Query = query, Limit = limit });
        return Ok(result);
    }
}
