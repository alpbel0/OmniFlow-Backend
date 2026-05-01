using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.DTOs.Providers;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.Providers.Queries.GetOriginCities;
using OmniFlow.Application.Features.Providers.Queries.GetProviderFlights;
using OmniFlow.Application.Features.Providers.Queries.GetProviderHotels;
using OmniFlow.Domain.Enums;

namespace OmniFlow.WebApi.Controllers.v1;

/// <summary>
/// Provider API endpoints — public access to flight, hotel, and origin city data.
/// No authentication required; useful for browsing before sign-up.
/// </summary>
[ApiController]
[Route("api/v1/providers")]
[AllowAnonymous]
public class ProvidersController : ControllerBase
{
    private ISender? _mediator;

    protected ISender Mediator =>
        _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    /// <summary>Get distinct origin cities available for departure.</summary>
    [HttpGet("origin-cities")]
    [ProducesResponseType(typeof(IReadOnlyList<OriginCityResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOriginCities()
    {
        var query = new GetOriginCitiesQuery();
        var result = await Mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get available flights for a route.
    /// </summary>
    /// <param name="fromCity">Departure city (required for outbound)</param>
    /// <param name="toCity">Arrival city (required for outbound)</param>
    /// <param name="date">Travel date (required for outbound)</param>
    /// <param name="personCount">Number of passengers (default 1)</param>
    /// <param name="isReturn">If true, resolves route from trip's last destination back to origin</param>
    /// <param name="tripId">Required when isReturn=true</param>
    [HttpGet("flights")]
    [ProducesResponseType(typeof(IReadOnlyList<ProviderFlightResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiException), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetFlights(
        [FromQuery] string? fromCity,
        [FromQuery] string? toCity,
        [FromQuery] DateOnly? date,
        [FromQuery] int personCount = 1,
        [FromQuery] bool isReturn = false,
        [FromQuery] Guid? tripId = null)
    {
        var query = new GetProviderFlightsQuery
        {
            FromCity = fromCity,
            ToCity = toCity,
            Date = date,
            PersonCount = personCount,
            IsReturn = isReturn,
            TripId = tripId
        };

        var result = await Mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get available hotels for a city with segment info and season-adjusted pricing.
    /// </summary>
    /// <param name="city">City name (required)</param>
    /// <param name="checkIn">Check-in date (required)</param>
    /// <param name="checkOut">Check-out date (required)</param>
    /// <param name="budgetTier">Filter by budget tier (optional)</param>
    /// <param name="personCount">Number of guests (default 1)</param>
    [HttpGet("hotels")]
    [ProducesResponseType(typeof(IReadOnlyList<ProviderHotelResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiException), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetHotels(
        [FromQuery] string city,
        [FromQuery] DateOnly checkIn,
        [FromQuery] DateOnly checkOut,
        [FromQuery] BudgetTier? budgetTier = null,
        [FromQuery] int personCount = 1)
    {
        var query = new GetProviderHotelsQuery
        {
            City = city,
            CheckIn = checkIn,
            CheckOut = checkOut,
            BudgetTier = budgetTier,
            PersonCount = personCount
        };

        var result = await Mediator.Send(query);
        return Ok(result);
    }
}
