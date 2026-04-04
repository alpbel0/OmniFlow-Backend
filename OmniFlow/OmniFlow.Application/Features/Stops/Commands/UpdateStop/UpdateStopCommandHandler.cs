using AutoMapper;
using MediatR;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;

namespace OmniFlow.Application.Features.Stops.Commands.UpdateStop;

public class UpdateStopCommandHandler : IRequestHandler<UpdateStopCommand, Unit>
{
    private readonly ITripRepositoryAsync _tripRepository;
    private readonly IStopRepositoryAsync _stopRepository;
    private readonly IAuthenticatedUserService _authenticatedUserService;
    private readonly IMapper _mapper;

    public UpdateStopCommandHandler(
        ITripRepositoryAsync tripRepository,
        IStopRepositoryAsync stopRepository,
        IAuthenticatedUserService authenticatedUserService,
        IMapper mapper)
    {
        _tripRepository = tripRepository;
        _stopRepository = stopRepository;
        _authenticatedUserService = authenticatedUserService;
        _mapper = mapper;
    }

    public async Task<Unit> Handle(UpdateStopCommand request, CancellationToken cancellationToken)
    {
        // Get trip with owner for authorization
        var trip = await _tripRepository.GetByIdWithOwnerAsync(request.TripId);

        if (trip == null)
        {
            throw new EntityNotFoundException("Trip", request.TripId);
        }

        // Owner authorization
        var currentUserId = Guid.Parse(_authenticatedUserService.UserId);
        if (trip.OwnerId != currentUserId)
        {
            throw new ForbiddenException("You are not authorized to update stops in this trip.");
        }

        // Get stop with Place navigation
        var stop = await _stopRepository.GetByIdWithPlaceAsync(request.StopId);

        if (stop == null)
        {
            throw new EntityNotFoundException("Stop", request.StopId);
        }

        // Verify stop belongs to the trip
        if (stop.TripId != request.TripId)
        {
            throw new ApiException("Stop does not belong to the specified trip.", 400);
        }

        // Time lock protection: prevent time-related field changes
        if (stop.IsTimeLocked)
        {
            if (request.DayNumber.HasValue || request.ArrivalTime.HasValue ||
                request.DurationMinutes.HasValue || request.IsTimeLocked.HasValue)
            {
                throw new ApiException("Time-locked stops cannot have time-related fields changed.", 400);
            }
        }

        // Apply updates using AutoMapper
        _mapper.Map(request, stop);
        await _stopRepository.UpdateAsync(stop);

        return Unit.Value;
    }
}