using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities;

public class ProviderFlight : BaseEntity
{
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

	public decimal Price { get; set; }

	public string CurrencyCode { get; set; } = string.Empty;

	public int? AvailableSeats { get; set; }

	public string ProviderName { get; set; } = string.Empty;
}
