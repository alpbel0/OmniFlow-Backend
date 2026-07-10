using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.DTOs.TimelineEntries;
using OmniFlow.Application.Features.TimelineEntries.Commands.CreateTimelineEntry;
using OmniFlow.Application.Features.TimelineEntries.Commands.DeleteTimelineEntry;
using OmniFlow.Application.Features.TimelineEntries.Commands.MarkEntryVisited;
using OmniFlow.Application.Features.TimelineEntries.Commands.ReorderTimelineEntries;
using OmniFlow.Application.Features.TimelineEntries.Commands.UpdateTimelineEntry;
using OmniFlow.Application.Features.TimelineEntries.Queries.GetTimeline;

namespace OmniFlow.WebApi.Controllers.v1;

/// <summary>
/// Timeline API endpoints — CRUD operations for trip timeline entries (places, flights, transport, accommodation, events).
/// GET is public for published trips; POST/PUT/DELETE require auth and ownership.
/// </summary>
public class TimelineController : BaseApiController
{
    /// <summary>Get all timeline entries for a trip. Published trips are public; Draft trips are owner-only.</summary>
    [HttpGet("~/api/v1/trips/{tripId:guid}/timeline")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<TimelineEntryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTimeline(
        [FromRoute] Guid tripId,
        [FromQuery] Guid? destinationId = null)
    {
        var query = new GetTimelineQuery(tripId, destinationId);
        var result = await Mediator.Send(query);
        return Ok(result);
    }

    /// <summary>Add a new timeline entry to a draft trip (Place, CustomFlight, CustomTransport, CustomAccommodation, CustomEvent).</summary>
    [HttpPost("~/api/v1/trips/{tripId:guid}/timeline/entry")]
    [ProducesResponseType(typeof(TimelineEntryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateEntry(
        [FromRoute] Guid tripId,
        [FromBody] CreateTimelineEntryRequest request)
    {
        var command = new CreateTimelineEntryCommand(
            TripId: tripId,
            DestinationId: request.DestinationId,
            DayNumber: request.DayNumber,
            EntryType: request.EntryType,
            PlaceId: request.PlaceId,
            CustomName: request.CustomName,
            CustomCategory: request.CustomCategory,
            CustomPhotoUrl: request.CustomPhotoUrl,
            CustomLatitude: request.CustomLatitude,
            CustomLongitude: request.CustomLongitude,
            CustomDescription: request.CustomDescription,
            StartTime: request.StartTime,
            DurationMinutes: request.DurationMinutes,
            FlightFromAirport: request.FlightFromAirport,
            FlightToAirport: request.FlightToAirport,
            FlightFromCity: request.FlightFromCity,
            FlightToCity: request.FlightToCity,
            FlightDepartureAt: request.FlightDepartureAt,
            FlightArrivalAt: request.FlightArrivalAt,
            Airline: request.Airline,
            FlightNumber: request.FlightNumber,
            TransportType: request.TransportType,
            TransportFromStation: request.TransportFromStation,
            TransportToStation: request.TransportToStation,
            TransportCompany: request.TransportCompany,
            AccommodationCheckIn: request.AccommodationCheckIn,
            AccommodationCheckOut: request.AccommodationCheckOut,
            AccommodationAddress: request.AccommodationAddress,
            Price: request.Price,
            CurrencyCode: request.CurrencyCode,
            ProviderFlightId: request.ProviderFlightId,
            ProviderHotelId: request.ProviderHotelId,
            Notes: request.Notes,
            PlanningSlotKey: request.PlanningSlotKey,
            IsLocked: request.IsLocked
        );

        var result = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetTimeline), new { tripId }, result);
    }

    /// <summary>Update an existing timeline entry.</summary>
    [HttpPut("~/api/v1/trips/{tripId:guid}/timeline/entry/{entryId:guid}")]
    [ProducesResponseType(typeof(TimelineEntryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEntry(
        [FromRoute] Guid tripId,
        [FromRoute] Guid entryId,
        [FromBody] UpdateTimelineEntryRequest request)
    {
        var command = new UpdateTimelineEntryCommand(
            Id: entryId,
            DestinationId: request.DestinationId,
            DayNumber: request.DayNumber,
            PlaceId: request.PlaceId,
            CustomName: request.CustomName,
            CustomCategory: request.CustomCategory,
            CustomPhotoUrl: request.CustomPhotoUrl,
            CustomLatitude: request.CustomLatitude,
            CustomLongitude: request.CustomLongitude,
            CustomDescription: request.CustomDescription,
            StartTime: request.StartTime,
            DurationMinutes: request.DurationMinutes,
            FlightFromAirport: request.FlightFromAirport,
            FlightToAirport: request.FlightToAirport,
            FlightFromCity: request.FlightFromCity,
            FlightToCity: request.FlightToCity,
            FlightDepartureAt: request.FlightDepartureAt,
            FlightArrivalAt: request.FlightArrivalAt,
            Airline: request.Airline,
            FlightNumber: request.FlightNumber,
            TransportType: request.TransportType,
            TransportFromStation: request.TransportFromStation,
            TransportToStation: request.TransportToStation,
            TransportCompany: request.TransportCompany,
            AccommodationCheckIn: request.AccommodationCheckIn,
            AccommodationCheckOut: request.AccommodationCheckOut,
            AccommodationAddress: request.AccommodationAddress,
            Price: request.Price,
            CurrencyCode: request.CurrencyCode,
            ProviderFlightId: request.ProviderFlightId,
            ProviderHotelId: request.ProviderHotelId,
            Notes: request.Notes,
            IsLocked: request.IsLocked
        );

        var result = await Mediator.Send(command);
        return Ok(result);
    }

    /// <summary>Remove a timeline entry (soft delete). Locked entries cannot be deleted.</summary>
    [HttpDelete("~/api/v1/trips/{tripId:guid}/timeline/entry/{entryId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEntry(
        [FromRoute] Guid tripId,
        [FromRoute] Guid entryId)
    {
        var command = new DeleteTimelineEntryCommand(entryId);
        await Mediator.Send(command);
        return NoContent();
    }

    /// <summary>Reorder a timeline entry within the same destination and day.</summary>
    [HttpPut("~/api/v1/trips/{tripId:guid}/timeline/reorder")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReorderEntry(
        [FromRoute] Guid tripId,
        [FromBody] ReorderTimelineEntriesRequest request)
    {
        var command = new ReorderTimelineEntriesCommand(
            TripId: tripId,
            DestinationId: request.DestinationId,
            EntryId: request.EntryId,
            BeforeEntryId: request.BeforeEntryId,
            AfterEntryId: request.AfterEntryId
        );

        await Mediator.Send(command);
        return NoContent();
    }

    /// <summary>Mark a timeline entry as visited or unvisited.</summary>
    [HttpPut("~/api/v1/trips/{tripId:guid}/timeline/entry/{entryId:guid}/visited")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkVisited(
        [FromRoute] Guid tripId,
        [FromRoute] Guid entryId,
        [FromBody] MarkVisitedRequest request)
    {
        var command = new MarkEntryVisitedCommand(entryId, request.IsVisited);
        await Mediator.Send(command);
        return NoContent();
    }
}

public class MarkVisitedRequest
{
    public bool IsVisited { get; set; }
}
