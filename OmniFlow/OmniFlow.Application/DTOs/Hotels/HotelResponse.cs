using System.ComponentModel.DataAnnotations;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.DTOs.Hotels;

public class HotelResponse
{
    public Guid Id { get; set; }
    public Guid TripId { get; set; }
    public Guid? PlaceId { get; set; }

    // Hotel details
    public string? HotelName { get; set; }
    public string? HotelAddress { get; set; }
    public string? HotelPhone { get; set; }
    public string? ProviderUrl { get; set; }
    public double? HotelLatitude { get; set; }
    public double? HotelLongitude { get; set; }
    public int? Stars { get; set; }
    public RoomType RoomType { get; set; }
    public bool BreakfastIncluded { get; set; }
    public CancellationPolicy CancellationPolicy { get; set; }

    // Dates
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public string CheckInFormatted => CheckIn.ToString("dd MMM yyyy");
    public string CheckOutFormatted => CheckOut.ToString("dd MMM yyyy");
    public int NightsCount => (CheckOut - CheckIn).Days;

    // Pricing
    public decimal PricePerNight { get; set; }
    public decimal TotalPrice { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;

    // Booking status
    public bool IsBooked { get; set; }
    public DateTime? BookedAt { get; set; }
    public string? BookingReference { get; set; }

    // Status
    public HotelStatus Status { get; set; }
    public HotelDataSource DataSource { get; set; }
}