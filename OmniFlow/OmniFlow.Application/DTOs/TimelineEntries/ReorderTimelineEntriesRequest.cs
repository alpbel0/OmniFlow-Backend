namespace OmniFlow.Application.DTOs.TimelineEntries;

public class ReorderTimelineEntriesRequest
{
    public Guid TripId { get; set; }
    public Guid DestinationId { get; set; }
    public Guid EntryId { get; set; }
    public Guid? BeforeEntryId { get; set; }
    public Guid? AfterEntryId { get; set; }
}