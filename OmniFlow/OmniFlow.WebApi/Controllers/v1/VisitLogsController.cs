using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.DTOs.VisitLogs;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.VisitLogs.Commands.CreateVisitLog;
using OmniFlow.Application.Features.VisitLogs.Commands.DeleteVisitLog;
using OmniFlow.Application.Features.VisitLogs.Commands.UpdateVisitLog;
using OmniFlow.Application.Features.VisitLogs.Queries.GetVisitLogs;
using OmniFlow.Application.Wrappers;

namespace OmniFlow.WebApi.Controllers.v1;

[Route("api/v1/trips/{tripId:guid}/visit-logs")]
public sealed class VisitLogsController : BaseApiController
{
    [HttpPost]
    [ProducesResponseType(typeof(VisitLogResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        Guid tripId,
        [FromBody] CreateVisitLogRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new CreateVisitLogCommand
        {
            TripId = tripId,
            TimelineEntryId = request.TimelineEntryId,
            PlaceId = request.PlaceId,
            TripDestinationId = request.TripDestinationId,
            VisitedAt = request.VisitedAt,
            ActualCost = request.ActualCost,
            CurrencyCode = request.CurrencyCode,
            Rating = request.Rating,
            Note = request.Note
        }, cancellationToken);
        return CreatedAtAction(nameof(Get), new { tripId, visitLogId = result.Id }, result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<VisitLogResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        Guid tripId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? tripDestinationId = null,
        [FromQuery] string? source = null,
        [FromQuery] string? visitedFrom = null,
        [FromQuery] string? visitedTo = null,
        [FromQuery] string sort = "visitedAtDesc",
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetVisitLogsQuery(
            tripId, pageNumber, pageSize, tripDestinationId, source,
            ParseUtcDateFilter(visitedFrom, nameof(visitedFrom)),
            ParseUtcDateFilter(visitedTo, nameof(visitedTo)),
            sort), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{visitLogId:guid}")]
    [ProducesResponseType(typeof(VisitLogResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(
        Guid tripId,
        Guid visitLogId,
        [FromBody] UpdateVisitLogRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new UpdateVisitLogCommand(
            tripId, visitLogId, request.TripDestinationId, request.VisitedAt,
            request.ActualCost, request.CurrencyCode, request.Rating, request.Note), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{visitLogId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid tripId, Guid visitLogId, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteVisitLogCommand(tripId, visitLogId), cancellationToken);
        return NoContent();
    }

    private static DateTime? ParseUtcDateFilter(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
            return parsed.UtcDateTime;

        throw new ApiException(
            $"Query parameter '{parameterName}' must be an ISO 8601 date-time.",
            StatusCodes.Status400BadRequest,
            "INVALID_DATE_FILTER");
    }
}
