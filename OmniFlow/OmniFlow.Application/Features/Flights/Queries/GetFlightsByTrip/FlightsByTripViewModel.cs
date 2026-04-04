using OmniFlow.Application.DTOs.Flights;

namespace OmniFlow.Application.Features.Flights.Queries.GetFlightsByTrip;

public class FlightsByTripViewModel
{
    public IReadOnlyList<FlightResponse> OutboundFlights { get; set; } = new List<FlightResponse>();
    public IReadOnlyList<FlightResponse> ReturnFlights { get; set; } = new List<FlightResponse>();
}