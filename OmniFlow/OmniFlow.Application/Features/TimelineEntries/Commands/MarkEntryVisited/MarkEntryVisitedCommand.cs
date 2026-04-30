using MediatR;

namespace OmniFlow.Application.Features.TimelineEntries.Commands.MarkEntryVisited;

public record MarkEntryVisitedCommand(Guid EntryId, bool IsVisited) : IRequest<Unit>;
