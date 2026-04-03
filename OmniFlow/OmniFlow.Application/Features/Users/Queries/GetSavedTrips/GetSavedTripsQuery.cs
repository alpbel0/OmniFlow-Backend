using MediatR;
using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.Wrappers;

namespace OmniFlow.Application.Features.Users.Queries.GetSavedTrips;

/// <summary>
/// Query to get authenticated user's saved trips with pagination.
/// </summary>
public class GetSavedTripsQuery : IRequest<PagedResponse<SavedTripResponse>>
{
    public GetSavedTripsParameter Parameter { get; }

    public GetSavedTripsQuery(GetSavedTripsParameter parameter)
    {
        Parameter = parameter;
    }
}