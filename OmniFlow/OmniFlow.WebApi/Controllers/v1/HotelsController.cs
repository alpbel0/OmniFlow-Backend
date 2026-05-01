using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.Features.Hotels.Queries.GetHotelsByTrip;
using OmniFlow.Application.DTOs.Hotels;

namespace OmniFlow.WebApi.Controllers.v1;

/// <summary>
/// Hotels API endpoints - nested under Trips.
/// All endpoints require authentication.
/// Route: api/v1/trips/{tripId}/hotels
/// </summary>
[ApiController]
[Route("api/v1/trips/{tripId:guid}/hotels")]
public class HotelsController : BaseApiController
{
	/// <summary>Get all hotels for a trip, sorted by check-in date.</summary>
    /// <remarks>
    /// Authorization: Published trips are public, Draft/Archived are owner-only.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(HotelsByTripViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHotels([FromRoute] Guid tripId)
    {
        var query = new GetHotelsByTripQuery(tripId);
        var result = await Mediator.Send(query);
        return Ok(result);
    }
}
