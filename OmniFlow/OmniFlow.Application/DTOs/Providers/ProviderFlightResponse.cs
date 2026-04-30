using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.DTOs.Providers;

public class ProviderFlightResponse
{
    public Guid Id { get; set; }
    public string FlightNumber { get; set; } = string.Empty;
    public string Airline { get; set; } = string.Empty;
    public string? AirlineLogoUrl { get; set; }
    public string DepartureCity { get; set; } = string.Empty;
    public string ArrivalCity { get; set; } = string.Empty;
    public string DepartureAirportCode { get; set; } = string.Empty;
    public string ArrivalAirportCode { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public int DurationMinutes { get; set; }

    public decimal BasePrice { get; set; }
    public decimal SeasonAdjustedPrice { get; set; }
    public decimal SeasonMultiplier { get; set; }
    public decimal TotalPrice { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;

    public int? AvailableSeats { get; set; }
    public string ProviderName { get; set; } = string.Empty;
}