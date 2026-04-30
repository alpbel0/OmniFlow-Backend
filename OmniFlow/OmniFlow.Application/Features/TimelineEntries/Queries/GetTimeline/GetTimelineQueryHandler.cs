using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.DTOs.TimelineEntries;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.TimelineEntries.Queries.GetTimeline;

public class GetTimelineQueryHandler : IRequestHandler<GetTimelineQuery, List<TimelineEntryResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITimelineEntryRepositoryAsync _timelineRepo;
    private readonly IAuthenticatedUserService _authService;
    private readonly IMapper _mapper;

    public GetTimelineQueryHandler(
        IApplicationDbContext context,
        ITimelineEntryRepositoryAsync timelineRepo,
        IAuthenticatedUserService authService,
        IMapper mapper)
    {
        _context = context;
        _timelineRepo = timelineRepo;
        _authService = authService;
        _mapper = mapper;
    }

    public async Task<List<TimelineEntryResponse>> Handle(GetTimelineQuery request, CancellationToken cancellationToken)
    {
        // 1. Load trip
        var trip = await _context.Trips
            .FirstOrDefaultAsync(t => t.Id == request.TripId && t.DeletedAt == null, cancellationToken)
            ?? throw new EntityNotFoundException("Trip", request.TripId);

        // 2. Authorization: Published = public, Draft/Archived = owner only
        if (trip.Status != TripStatus.Published)
        {
            var currentUserId = Guid.Parse(_authService.UserId);
            if (trip.OwnerId != currentUserId)
                throw new ForbiddenException("You are not authorized to view this trip's timeline.");
        }

        // 3. Load entries
        IReadOnlyList<Domain.Entities.TimelineEntry> entries;
        if (request.DestinationId.HasValue)
        {
            entries = await _timelineRepo.GetByDestinationAsync(request.DestinationId.Value);
        }
        else
        {
            entries = await _timelineRepo.GetByTripAsync(request.TripId);
        }

        return _mapper.Map<List<TimelineEntryResponse>>(entries);
    }
}
