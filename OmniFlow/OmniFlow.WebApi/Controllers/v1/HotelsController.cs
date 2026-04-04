using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.DTOs.Hotels;
using OmniFlow.Application.Features.Hotels.Commands.SelectHotel;
using OmniFlow.Application.Features.Hotels.Queries.GetHotelsByTrip;

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

    /// <summary>Select/book a hotel for the trip.</summary>
    /// <remarks>
    /// If another hotel was previously booked, it will be unbooked automatically.
    /// Only one hotel can be booked at a time for a trip.
    /// </remarks>
    [HttpPost("select")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SelectHotel(
        [FromRoute] Guid tripId,
        [FromBody] SelectHotelRequest request)
    {
        var command = new SelectHotelCommand
        {
            TripId = tripId,
            HotelId = request.HotelId
        };

        await Mediator.Send(command);
        return NoContent();
    }
}