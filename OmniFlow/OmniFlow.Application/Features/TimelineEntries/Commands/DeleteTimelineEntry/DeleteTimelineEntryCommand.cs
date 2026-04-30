using MediatR;

namespace OmniFlow.Application.Features.TimelineEntries.Commands.DeleteTimelineEntry;

public record DeleteTimelineEntryCommand(Guid Id) : IRequest<Unit>;
