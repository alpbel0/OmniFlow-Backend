using AutoMapper;
using MediatR;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.Stops.Commands.CreateStop;

public class CreateStopCommandHandler : IRequestHandler<CreateStopCommand, Guid>
{
    private readonly ITripRepositoryAsync _tripRepository;
    private readonly IStopRepositoryAsync _stopRepository;
    private readonly IAuthenticatedUserService _authenticatedUserService;
    private readonly IMapper _mapper;

    public CreateStopCommandHandler(
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

    public async Task<Guid> Handle(CreateStopCommand request, CancellationToken cancellationToken)
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
            throw new ForbiddenException("You are not authorized to add stops to this trip.");
        }

        // Calculate OrderIndex using LexoRank pattern
        var lastStop = await _stopRepository.GetLastStopInDayAsync(request.TripId, request.DayNumber);
        var orderIndex = lastStop != null ? lastStop.OrderIndex + 1000.0 : 1000.0;

        // Create stop entity
        var stop = _mapper.Map<Stop>(request);
        stop.TripId = request.TripId;
        stop.OrderIndex = orderIndex;
        stop.AddedBy = StopAddedBy.User;

        // Set default currency code if not provided
        if (string.IsNullOrEmpty(stop.CurrencyCode))
        {
            stop.CurrencyCode = "USD";
        }

        await _stopRepository.AddAsync(stop);

        return stop.Id;
    }
}