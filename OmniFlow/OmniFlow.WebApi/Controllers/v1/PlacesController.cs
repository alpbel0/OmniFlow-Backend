using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.DTOs.Places;
using OmniFlow.Application.Features.Places.Commands.CreatePlace;
using OmniFlow.Application.Features.Places.Queries.GetAllPlaces;
using OmniFlow.Application.Features.Places.Queries.GetPlaceById;
using OmniFlow.Application.Features.Places.Queries.GetPlacesByCity;
using OmniFlow.Application.Wrappers;

namespace OmniFlow.WebApi.Controllers.v1;

/// <summary>
/// Places API endpoints - CRUD operations for travel places.
/// GET endpoints available to all authenticated users.
/// POST endpoint restricted to Admin role.
/// </summary>
public class PlacesController : BaseApiController
{
	private readonly IValidator<CreatePlaceRequest> _createValidator;

	public PlacesController(IValidator<CreatePlaceRequest> createValidator)
	{
		_createValidator = createValidator;
	}

	/// <summary>Get all places with pagination.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<PlaceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var query = new GetAllPlacesQuery { PageNumber = pageNumber, PageSize = pageSize };
        var result = await Mediator.Send(query);
        return Ok(result);
    }

    /// <summary>Get a specific place by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PlaceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var query = new GetPlaceByIdQuery(id);
        var result = await Mediator.Send(query);
        return Ok(result);
    }

    /// <summary>Get places filtered by city with pagination.</summary>
    [HttpGet("city/{city}")]
    [ProducesResponseType(typeof(PagedResponse<PlaceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetByCity(
        [FromRoute] string city,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new GetPlacesByCityQuery { City = city, PageNumber = pageNumber, PageSize = pageSize };
        var result = await Mediator.Send(query);
        return Ok(result);
    }

    /// <summary>Create a new place (Admin only).</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreatePlaceRequest request)
    {
        var command = new CreatePlaceCommand
        {
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            PhotoUrl = request.PhotoUrl,
            Phone = request.Phone,
            WebsiteUrl = request.WebsiteUrl,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Address = request.Address,
            City = request.City,
            Country = request.Country,
            Timezone = request.Timezone,
            GooglePlaceId = request.GooglePlaceId,
            EstimatedPrice = request.EstimatedPrice,
            CurrencyCode = request.CurrencyCode,
            IsFree = request.IsFree,
            BudgetTiers = request.BudgetTiers,
            TravelStyles = request.TravelStyles,
            DurationMinutes = request.DurationMinutes,
            Rating = request.Rating,
            OpeningHours = request.OpeningHours,
            BestMonths = request.BestMonths
        };

        var placeId = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = placeId }, placeId);
    }
}