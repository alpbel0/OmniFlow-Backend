using MediatR;

namespace OmniFlow.Application.Features.TimelineEntries.Queries.GetTimeline;

public record GetTimelineQuery(Guid TripId, Guid? DestinationId = null) : IRequest<List<OmniFlow.Application.DTOs.TimelineEntries.TimelineEntryResponse>>;
