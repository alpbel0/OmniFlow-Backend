using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.DTOs.TripDestinations;
using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.Features.TripDestinations.Commands.CreateTripDestination;
using OmniFlow.Application.Features.TripDestinations.Commands.DeleteTripDestination;
using OmniFlow.Application.Features.TripDestinations.Commands.UpdateTripDestination;
using OmniFlow.Application.Features.TripDestinations.Queries.GetTripDestinations;

namespace OmniFlow.WebApi.Controllers.v1;

/// <summary>
/// Trip Destination API endpoints — CRUD operations for trip legs.
/// GET is public for published trips; POST/PUT/DELETE require auth and ownership.
/// </summary>
public class TripDestinationsController : BaseApiController
{
    /// <summary>Get all destinations for a trip (ordered by OrderIndex). Published trips are public; Draft trips are owner-only.</summary>
    [HttpGet("~/api/v1/trips/{tripId:guid}/destinations")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<TripDestinationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByTrip([FromRoute] Guid tripId)
    {
        var query = new GetTripDestinationsQuery(tripId);
        var result = await Mediator.Send(query);
        return Ok(result);
    }

    /// <summary>Add a new destination to a draft trip.</summary>
    [HttpPost("~/api/v1/trips/{tripId:guid}/destinations")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(
        [FromRoute] Guid tripId,
        [FromBody] CreateTripDestinationRequest request)
    {
        var command = new CreateTripDestinationCommand
        {
            TripId = tripId,
            City = request.City,
            Country = request.Country,
            ArrivalDate = request.ArrivalDate,
            DepartureDate = request.DepartureDate,
            OrderIndex = request.OrderIndex
        };

        var destinationId = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetByTrip), new { tripId }, destinationId);
    }

    /// <summary>Update an existing destination.</summary>
    [HttpPut("~/api/v1/trips/{tripId:guid}/destinations/{destinationId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid tripId,
        [FromRoute] Guid destinationId,
        [FromBody] UpdateTripDestinationRequest request)
    {
        var command = new UpdateTripDestinationCommand
        {
            DestinationId = destinationId,
            City = request.City,
            Country = request.Country,
            ArrivalDate = request.ArrivalDate,
            DepartureDate = request.DepartureDate,
            OrderIndex = request.OrderIndex
        };

        await Mediator.Send(command);
        return NoContent();
    }

    /// <summary>Remove a destination from a trip (soft delete).</summary>
    [HttpDelete("~/api/v1/trips/{tripId:guid}/destinations/{destinationId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid tripId,
        [FromRoute] Guid destinationId)
    {
        var command = new DeleteTripDestinationCommand { DestinationId = destinationId };
        await Mediator.Send(command);
        return NoContent();
    }
}
