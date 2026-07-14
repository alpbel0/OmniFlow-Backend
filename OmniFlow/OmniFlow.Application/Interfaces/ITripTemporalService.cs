using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Interfaces;

public interface ITripTemporalService
{
    TripExecutionStateResult GetExecutionState(Trip trip);
    DateOnly GetLocalDate(DateTime utcInstant, string timezone);
}

public sealed record TripExecutionStateResult(
    TripExecutionState? State,
    bool IsTimezoneComplete);
