using FluentValidation;

namespace OmniFlow.Application.DTOs.TimelineEntries;

public class ReorderTimelineEntriesRequestValidator : AbstractValidator<ReorderTimelineEntriesRequest>
{
    public ReorderTimelineEntriesRequestValidator()
    {
        RuleFor(x => x.TripId).NotEmpty().WithMessage("TripId is required.");
        RuleFor(x => x.DestinationId).NotEmpty().WithMessage("DestinationId is required.");
        RuleFor(x => x.EntryId).NotEmpty().WithMessage("EntryId is required.");

        RuleFor(x => x.EntryId)
            .Must((request, entryId) => entryId != request.BeforeEntryId)
            .When(x => x.BeforeEntryId.HasValue)
            .WithMessage("EntryId cannot be the same as BeforeEntryId.");

        RuleFor(x => x.EntryId)
            .Must((request, entryId) => entryId != request.AfterEntryId)
            .When(x => x.AfterEntryId.HasValue)
            .WithMessage("EntryId cannot be the same as AfterEntryId.");
    }
}