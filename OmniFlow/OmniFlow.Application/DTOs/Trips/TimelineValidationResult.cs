namespace OmniFlow.Application.DTOs.Trips;

public class TimelineValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }

    public static TimelineValidationResult Valid() =>
        new() { IsValid = true };

    public static TimelineValidationResult Invalid(string message, string code) =>
        new() { IsValid = false, ErrorMessage = message, ErrorCode = code };
}
