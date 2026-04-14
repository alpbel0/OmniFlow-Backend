using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.Features.Trips.Queries.ExploreTrips;
using OmniFlow.Application.Features.Trips.Queries.GetFeaturedTrips;
using OmniFlow.Domain.Enums;

namespace OmniFlow.WebApi.Controllers.v1;

/// <summary>
/// Explore API endpoints - Browse published trips with filtering and cursor pagination.
/// Authentication is optional - IsUpvoted/IsSaved flags are null for unauthenticated users.
/// </summary>
[Route("api/v1/explore")]
public class ExploreController : BaseApiController
{
    /// <summary>
    /// Explore published trips with optional filters and cursor pagination.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ExploreTripsViewModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> Explore(
        [FromQuery] string? searchTerm,
        [FromQuery] string? city,
        [FromQuery] string? country,
        [FromQuery] BudgetTier? budgetTier,
        [FromQuery] TravelStyle? travelStyle,
        [FromQuery] string? tags,
        [FromQuery] string sortBy = "popularity_score",
        [FromQuery] int pageSize = 10,
        [FromQuery] string? cursor = null)
    {
        var tagList = tags?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

        var parameter = new ExploreTripsParameter
        {
            SearchTerm = searchTerm,
            City = city,
            Country = country,
            BudgetTier = budgetTier,
            TravelStyle = travelStyle,
            Tags = tagList,
            SortBy = sortBy,
            PageSize = pageSize,
            Cursor = cursor
        };

        var query = new ExploreTripsQuery(parameter);
        var result = await Mediator.Send(query);
        return Ok(result);
    }

    /// <summary>Son 7 günde oluşturulmuş yayınlanmış gezilerden etkileşim skoruna göre öne çıkanlar.</summary>
    [HttpGet("featured")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<FeaturedTripResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFeatured([FromQuery] int limit = 6)
    {
        var result = await Mediator.Send(new GetFeaturedTripsQuery { Limit = limit });
        return Ok(result);
    }
}
