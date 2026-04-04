using MediatR;

namespace OmniFlow.Application.Features.Hotels.Commands.SelectHotel;

public class SelectHotelCommand : IRequest<Unit>
{
    public Guid TripId { get; set; }
    public Guid HotelId { get; set; }
}