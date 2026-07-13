using OmniFlow.Application.Interfaces;

namespace OmniFlow.Infrastructure.Services;

public sealed class SystemDateTimeService : IDateTimeService
{
	public DateTime NowUtc => DateTime.UtcNow;
}
