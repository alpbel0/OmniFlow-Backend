using MediatR;
using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;

namespace OmniFlow.Application.Features.Trips.Commands.UploadTripCoverPhoto;

public class UploadTripCoverPhotoCommandHandler : IRequestHandler<UploadTripCoverPhotoCommand, UploadTripCoverPhotoResponse>
{
    private readonly ITripRepositoryAsync _tripRepository;
    private readonly IAuthenticatedUserService _authenticatedUserService;
    private readonly IBlobService _blobService;

    public UploadTripCoverPhotoCommandHandler(
        ITripRepositoryAsync tripRepository,
        IAuthenticatedUserService authenticatedUserService,
        IBlobService blobService)
    {
        _tripRepository = tripRepository;
        _authenticatedUserService = authenticatedUserService;
        _blobService = blobService;
    }

    public async Task<UploadTripCoverPhotoResponse> Handle(
        UploadTripCoverPhotoCommand request,
        CancellationToken cancellationToken)
    {
        var trip = await _tripRepository.GetByIdWithOwnerAsync(request.TripId);
        if (trip is null)
            throw new EntityNotFoundException("Trip", request.TripId);

        var currentUserId = Guid.Parse(_authenticatedUserService.UserId);
        if (trip.OwnerId != currentUserId)
            throw new ForbiddenException("You are not authorized to update this trip cover photo.");

        var coverPhotoUrl = await _blobService.UploadAsync(
            request.FileStream,
            request.ContentType,
            request.FileName,
            "trip-cover-photos",
            cancellationToken);

        trip.CoverPhotoUrl = coverPhotoUrl;
        await _tripRepository.UpdateAsync(trip);

        return new UploadTripCoverPhotoResponse { CoverPhotoUrl = coverPhotoUrl };
    }
}
