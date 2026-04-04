using OmniFlow.Application.DTOs.Hotels;

namespace OmniFlow.Application.Features.Hotels.Queries.GetHotelsByTrip;

public class HotelsByTripViewModel
{
    public IReadOnlyList<HotelResponse> Hotels { get; set; } = new List<HotelResponse>();
}