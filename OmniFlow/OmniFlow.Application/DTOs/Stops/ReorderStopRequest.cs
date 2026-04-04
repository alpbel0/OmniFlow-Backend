namespace OmniFlow.Application.DTOs.Stops;

public class ReorderStopRequest
{
    public Guid StopId { get; set; }
    public int NewDayNumber { get; set; }
    public Guid? AfterStopId { get; set; }   // Insert after this stop
    public Guid? BeforeStopId { get; set; }  // Insert before this stop
}