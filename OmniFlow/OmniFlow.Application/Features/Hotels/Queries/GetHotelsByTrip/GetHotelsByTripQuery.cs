using MediatR;
using OmniFlow.Application.DTOs.Hotels;

namespace OmniFlow.Application.Features.Hotels.Queries.GetHotelsByTrip;

public class GetHotelsByTripQuery : IRequest<HotelsByTripViewModel>
{
    public Guid TripId { get; }

    public GetHotelsByTripQuery(Guid tripId)
    {
        TripId = tripId;
    }
}