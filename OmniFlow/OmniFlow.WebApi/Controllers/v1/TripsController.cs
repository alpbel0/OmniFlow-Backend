using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.Features.SavedTrips.Commands.SaveTrip;
using OmniFlow.Application.Features.SavedTrips.Commands.UnsaveTrip;
using OmniFlow.Application.Features.Trips.Commands.ArchiveTrip;
using OmniFlow.Application.Features.Trips.Commands.CreateTrip;
using OmniFlow.Application.Features.Trips.Commands.DeleteTrip;
using OmniFlow.Application.Features.Trips.Commands.ForkTrip;
using OmniFlow.Application.Features.Trips.Commands.PublishTrip;
using OmniFlow.Application.Features.Trips.Commands.RemoveUpvoteTrip;
using OmniFlow.Application.Features.Trips.Commands.UpdateTrip;
using OmniFlow.Application.Features.Trips.Commands.UpvoteTrip;
using OmniFlow.Application.Features.Trips.Queries.GetMyTrips;
using OmniFlow.Application.Features.Trips.Queries.GetTripById;
using OmniFlow.Domain.Enums;

namespace OmniFlow.WebApi.Controllers.v1;

/// <summary>
/// Trips API endpoints - CRUD operations for travel trips.
/// All endpoints require authentication.
/// </summary>
public class TripsController : BaseApiController
{
	private readonly IValidator<CreateTripRequest> _createValidator;
	private readonly IValidator<UpdateTripRequest> _updateValidator;

	public TripsController(
		IValidator<CreateTripRequest> createValidator,
		IValidator<UpdateTripRequest> updateValidator)
	{
		_createValidator = createValidator;
		_updateValidator = updateValidator;
	}

	/// <summary>Get authenticated user's trips with pagination.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(GetMyTripsViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyTrips(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] TripStatus? status = null)
    {
        var parameter = new GetMyTripsParameter
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Status = status
        };
        var query = new GetMyTripsQuery(parameter);
        var result = await Mediator.Send(query);
        return Ok(result);
    }

    /// <summary>Get a specific trip by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TripResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var query = new GetTripByIdQuery(id);
        var result = await Mediator.Send(query);
        return Ok(result);
    }

    /// <summary>Create a new trip.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateTripRequest request)
    {
        var command = new CreateTripCommand
        {
            Title = request.Title,
            Description = request.Description,
            City = request.City,
            Country = request.Country,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            PersonCount = request.PersonCount,
            BudgetTier = request.BudgetTier,
            TravelStyle = request.TravelStyle,
            UserBudget = request.UserBudget,
            CoverPhotoUrl = request.CoverPhotoUrl,
            Tags = request.Tags
        };

        var tripId = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = tripId }, tripId);
    }

    /// <summary>Update an existing trip (Draft only).</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateTripRequest request)
    {
        var command = new UpdateTripCommand
        {
            TripId = id,
            Title = request.Title,
            Description = request.Description,
            City = request.City,
            Country = request.Country,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            PersonCount = request.PersonCount,
            BudgetTier = request.BudgetTier,
            TravelStyle = request.TravelStyle,
            UserBudget = request.UserBudget,
            CoverPhotoUrl = request.CoverPhotoUrl,
            Tags = request.Tags
        };

        await Mediator.Send(command);
        return NoContent();
    }

    /// <summary>Delete a trip (soft delete).</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        var command = new DeleteTripCommand { TripId = id };
        await Mediator.Send(command);
        return NoContent();
    }

    /// <summary>Publish a draft trip.</summary>
    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Publish([FromRoute] Guid id)
    {
        var command = new PublishTripCommand { TripId = id };
        await Mediator.Send(command);
        return NoContent();
    }

    /// <summary>Archive a published trip.</summary>
    [HttpPost("{id:guid}/archive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Archive([FromRoute] Guid id)
    {
        var command = new ArchiveTripCommand { TripId = id };
        await Mediator.Send(command);
        return NoContent();
    }

    /// <summary>Upvote a published trip.</summary>
    [HttpPost("{id:guid}/upvote")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Upvote([FromRoute] Guid id)
    {
        var command = new UpvoteTripCommand { TripId = id };
        await Mediator.Send(command);
        return NoContent();
    }

    /// <summary>Remove upvote from a trip.</summary>
    [HttpDelete("{id:guid}/upvote")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveUpvote([FromRoute] Guid id)
    {
        var command = new RemoveUpvoteTripCommand { TripId = id };
        await Mediator.Send(command);
        return NoContent();
    }

    /// <summary>Save a trip to user's saved list.</summary>
    [HttpPost("{id:guid}/save")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Save([FromRoute] Guid id)
    {
        var command = new SaveTripCommand { TripId = id };
        await Mediator.Send(command);
        return NoContent();
    }

    /// <summary>Remove trip from user's saved list.</summary>
    [HttpDelete("{id:guid}/save")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unsave([FromRoute] Guid id)
    {
        var command = new UnsaveTripCommand { TripId = id };
        await Mediator.Send(command);
        return NoContent();
    }

    /// <summary>Fork a published trip (create a copy as Draft).</summary>
    [HttpPost("{id:guid}/fork")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Fork([FromRoute] Guid id)
    {
        var command = new ForkTripCommand { TripId = id };
        var forkedTripId = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = forkedTripId }, forkedTripId);
    }
}