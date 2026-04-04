using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.DTOs.Flights;
using OmniFlow.Application.Features.Flights.Commands.SelectFlight;
using OmniFlow.Application.Features.Flights.Queries.GetFlightsByTrip;

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

    /// <summary>Select/book a flight for the trip.</summary>
    /// <remarks>
    /// If another flight of the same direction was previously booked, it will be unbooked automatically.
    /// Only one flight per direction (Outbound/Return) can be booked at a time.
    /// </remarks>
    [HttpPost("select")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SelectFlight(
        [FromRoute] Guid tripId,
        [FromBody] SelectFlightRequest request)
    {
        var command = new SelectFlightCommand
        {
            TripId = tripId,
            FlightId = request.FlightId
        };

        await Mediator.Send(command);
        return NoContent();
    }
}