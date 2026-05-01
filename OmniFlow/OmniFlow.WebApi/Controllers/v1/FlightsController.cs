using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.Features.Flights.Queries.GetFlightsByTrip;
using OmniFlow.Application.DTOs.Flights;

namespace OmniFlow.WebApi.Controllers.v1;

/// <summary>
/// Flights API endpoints - nested under Trips.
/// All endpoints require authentication.
/// Route: api/v1/trips/{tripId}/flights
/// </summary>
[ApiController]
[Route("api/v1/trips/{tripId:guid}/flights")]
public class FlightsController : BaseApiController
{
	/// <summary>Get all flights for a trip, grouped by direction (Outbound/Return).</summary>
    /// <remarks>
    /// Authorization: Published trips are public, Draft/Archived are owner-only.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(FlightsByTripViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFlights([FromRoute] Guid tripId)
    {
        var query = new GetFlightsByTripQuery(tripId);
        var result = await Mediator.Send(query);
        return Ok(result);
    }
}
