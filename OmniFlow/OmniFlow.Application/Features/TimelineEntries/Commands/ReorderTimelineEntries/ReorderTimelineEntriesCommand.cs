using MediatR;

namespace OmniFlow.Application.Features.TimelineEntries.Commands.ReorderTimelineEntries;

public record ReorderTimelineEntriesCommand(
    Guid TripId,
    Guid DestinationId,
    Guid EntryId,
    Guid? BeforeEntryId,
    Guid? AfterEntryId
) : IRequest<Unit>;
