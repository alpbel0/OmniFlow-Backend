using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.DTOs.Stops;
using OmniFlow.Application.Features.Stops.Commands.CreateStop;
using OmniFlow.Application.Features.Stops.Commands.DeleteStop;
using OmniFlow.Application.Features.Stops.Commands.MarkStopVisited;
using OmniFlow.Application.Features.Stops.Commands.ReorderStops;
using OmniFlow.Application.Features.Stops.Commands.UpdateStop;
using OmniFlow.Application.Features.Stops.Queries.GetStopsByTrip;

namespace OmniFlow.WebApi.Controllers.v1;

/// <summary>
/// Stops API endpoints - nested under Trips.
/// All endpoints require authentication.
/// Route: api/v1/trips/{tripId}/stops
/// </summary>
[ApiController]
[Route("api/v1/trips/{tripId:guid}/stops")]
public class StopsController : BaseApiController
{
    /// <summary>Get all stops for a trip.</summary>
    /// <remarks>
    /// Authorization: Published trips are public, Draft/Archived are owner-only.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(List<StopResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStops([FromRoute] Guid tripId)
    {
        var query = new GetStopsByTripQuery(tripId);
        var result = await Mediator.Send(query);
        return Ok(result);
    }

    /// <summary>Create a new stop in a trip.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateStop(
        [FromRoute] Guid tripId,
        [FromBody] CreateStopRequest request)
    {
        var command = new CreateStopCommand
        {
            TripId = tripId,
            PlaceId = request.PlaceId,
            FallbackPlaceId = request.FallbackPlaceId,
            DayNumber = request.DayNumber,
            ArrivalTime = request.ArrivalTime,
            DurationMinutes = request.DurationMinutes,
            IsTimeLocked = request.IsTimeLocked,
            CustomName = request.CustomName,
            CustomCategory = request.CustomCategory,
            CustomPhotoUrl = request.CustomPhotoUrl,
            CustomLatitude = request.CustomLatitude,
            CustomLongitude = request.CustomLongitude,
            Notes = request.Notes,
            ActivityPrice = request.ActivityPrice,
            TransportPrice = request.TransportPrice,
            CurrencyCode = request.CurrencyCode,
            TransportFromPrevious = request.TransportFromPrevious,
            TravelTimeFromPrevious = request.TravelTimeFromPrevious
        };

        var stopId = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetStops), new { tripId }, stopId);
    }

    /// <summary>Update a stop.</summary>
    [HttpPut("{stopId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStop(
        [FromRoute] Guid tripId,
        [FromRoute] Guid stopId,
        [FromBody] UpdateStopRequest request)
    {
        var command = new UpdateStopCommand
        {
            TripId = tripId,
            StopId = stopId,
            PlaceId = request.PlaceId,
            FallbackPlaceId = request.FallbackPlaceId,
            DayNumber = request.DayNumber,
            ArrivalTime = request.ArrivalTime,
            DurationMinutes = request.DurationMinutes,
            IsTimeLocked = request.IsTimeLocked,
            CustomName = request.CustomName,
            CustomCategory = request.CustomCategory,
            CustomPhotoUrl = request.CustomPhotoUrl,
            CustomLatitude = request.CustomLatitude,
            CustomLongitude = request.CustomLongitude,
            Notes = request.Notes,
            BookingReference = request.BookingReference,
            ReservationNote = request.ReservationNote,
            ActivityPrice = request.ActivityPrice,
            TransportPrice = request.TransportPrice,
            CurrencyCode = request.CurrencyCode,
            TransportFromPrevious = request.TransportFromPrevious,
            TravelTimeFromPrevious = request.TravelTimeFromPrevious
        };

        await Mediator.Send(command);
        return NoContent();
    }

    /// <summary>Delete a stop (soft delete).</summary>
    [HttpDelete("{stopId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteStop(
        [FromRoute] Guid tripId,
        [FromRoute] Guid stopId)
    {
        var command = new DeleteStopCommand
        {
            TripId = tripId,
            StopId = stopId
        };

        await Mediator.Send(command);
        return NoContent();
    }

    /// <summary>Reorder stops within a trip.</summary>
    /// <remarks>
    /// LexoRank pattern: Provide AfterStopId and/or BeforeStopId to calculate new position.
    /// Time-locked stops cannot be reordered.
    /// </remarks>
    [HttpPut("reorder")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReorderStops(
        [FromRoute] Guid tripId,
        [FromBody] List<ReorderStopRequest> requests)
    {
        var command = new ReorderStopsCommand
        {
            TripId = tripId,
            Items = requests.Select(r => new ReorderStopItem
            {
                StopId = r.StopId,
                NewDayNumber = r.NewDayNumber,
                AfterStopId = r.AfterStopId,
                BeforeStopId = r.BeforeStopId
            }).ToList()
        };

        await Mediator.Send(command);
        return NoContent();
    }

    /// <summary>Mark a stop as visited.</summary>
    [HttpPost("{stopId:guid}/visited")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkVisited(
        [FromRoute] Guid tripId,
        [FromRoute] Guid stopId)
    {
        var command = new MarkStopVisitedCommand
        {
            TripId = tripId,
            StopId = stopId
        };

        await Mediator.Send(command);
        return NoContent();
    }
}