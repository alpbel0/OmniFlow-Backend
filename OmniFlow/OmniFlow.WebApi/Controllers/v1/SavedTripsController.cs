using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.Features.Users.Queries.GetSavedTrips;
using OmniFlow.Application.Wrappers;

namespace OmniFlow.WebApi.Controllers.v1;

/// <summary>
/// Saved Trips API endpoints - manage user's saved trips list.
/// All endpoints require authentication.
/// </summary>
[Route("api/v1/saved-trips")]
public class SavedTripsController : BaseApiController
{
    /// <summary>Get authenticated user's saved trips with pagination.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<SavedTripResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSavedTrips(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var parameter = new GetSavedTripsParameter
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        var query = new GetSavedTripsQuery(parameter);
        var result = await Mediator.Send(query);
        return Ok(result);
    }
}