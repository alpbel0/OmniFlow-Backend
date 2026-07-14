using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities;

public class ExchangeRateSnapshot : AuditableBaseEntity
{
    public string BaseCurrency { get; set; } = string.Empty;
    public string QuoteCurrency { get; set; } = string.Empty;
    public DateOnly RateDate { get; set; }
    public decimal Rate { get; set; }
    public string Provider { get; set; } = "ECB";
    public DateTime FetchedAtUtc { get; set; }
}
