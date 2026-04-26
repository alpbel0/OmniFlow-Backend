using OmniFlow.Domain.Common;
using OmniFlow.Domain.Exceptions;

namespace OmniFlow.Domain.Entities;

public class TripDestination : AuditableBaseEntity
{
	private DateOnly _arrivalDate;
	private DateOnly _departureDate;

	public Guid TripId { get; set; }

	public string City { get; set; } = string.Empty;

	public string Country { get; set; } = string.Empty;

	public DateOnly ArrivalDate
	{
		get => _arrivalDate;
		private set
		{
			_arrivalDate = value;
			RecalculateNightCount();
		}
	}

	public DateOnly DepartureDate
	{
		get => _departureDate;
		private set
		{
			_departureDate = value;
			RecalculateNightCount();
		}
	}

	public int OrderIndex { get; set; }

	public int NightCount { get; private set; }

	public Trip? Trip { get; set; }

	public ICollection<TimelineEntry> TimelineEntries { get; set; } = new List<TimelineEntry>();

	public TripDestination(DateOnly arrivalDate, DateOnly departureDate, string city, string country, int orderIndex)
	{
		if (departureDate < arrivalDate)
			throw new DomainException("DepartureDate cannot be earlier than ArrivalDate.");
		if (orderIndex < 1 || orderIndex > 3)
			throw new DomainException("OrderIndex must be between 1 and 3.");
		if (string.IsNullOrWhiteSpace(city))
			throw new DomainException("City is required.");
		if (string.IsNullOrWhiteSpace(country))
			throw new DomainException("Country is required.");

		City = city.Trim();
		Country = country.Trim();
		OrderIndex = orderIndex;
		_arrivalDate = arrivalDate;
		_departureDate = departureDate;
		RecalculateNightCount();
	}

	// EF Core parameterless constructor
	private TripDestination()
	{
	}

	public void UpdateDates(DateOnly arrivalDate, DateOnly departureDate)
	{
		if (departureDate < arrivalDate)
			throw new DomainException("DepartureDate cannot be earlier than ArrivalDate.");

		_arrivalDate = arrivalDate;
		_departureDate = departureDate;
		RecalculateNightCount();
	}

	public void UpdateCity(string city, string country)
	{
		if (string.IsNullOrWhiteSpace(city))
			throw new DomainException("City is required.");
		if (string.IsNullOrWhiteSpace(country))
			throw new DomainException("Country is required.");

		City = city.Trim();
		Country = country.Trim();
	}

	private void RecalculateNightCount()
	{
		NightCount = DepartureDate.DayNumber - ArrivalDate.DayNumber;
	}
}
