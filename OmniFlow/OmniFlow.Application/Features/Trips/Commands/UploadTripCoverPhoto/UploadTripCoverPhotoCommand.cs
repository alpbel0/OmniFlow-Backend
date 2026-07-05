using MediatR;
using OmniFlow.Application.DTOs.Trips;

namespace OmniFlow.Application.Features.Trips.Commands.UploadTripCoverPhoto;

public class UploadTripCoverPhotoCommand : IRequest<UploadTripCoverPhotoResponse>
{
    public Guid TripId { get; set; }
    public Stream FileStream { get; set; } = default!;
    public string ContentType { get; set; } = string.Empty;
    public string? FileName { get; set; }
}
