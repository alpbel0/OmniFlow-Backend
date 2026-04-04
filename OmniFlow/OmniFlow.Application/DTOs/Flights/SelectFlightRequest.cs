using System.ComponentModel.DataAnnotations;

namespace OmniFlow.Application.DTOs.Flights;

public class SelectFlightRequest
{
    [Required]
    public Guid FlightId { get; set; }
}