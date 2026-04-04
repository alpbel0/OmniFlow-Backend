using System.ComponentModel.DataAnnotations;

namespace OmniFlow.Application.DTOs.Hotels;

public class SelectHotelRequest
{
    [Required]
    public Guid HotelId { get; set; }
}