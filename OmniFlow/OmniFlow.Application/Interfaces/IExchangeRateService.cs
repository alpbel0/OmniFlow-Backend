namespace OmniFlow.Application.Interfaces;

public interface IExchangeRateService
{
    Task<ExchangeRateResult> GetRateAsync(
        string baseCurrency,
        string quoteCurrency,
        DateOnly? requestedDate,
        CancellationToken cancellationToken = default);
}

public sealed record ExchangeRateResult(
    string BaseCurrency,
    string QuoteCurrency,
    DateOnly RequestedDate,
    DateOnly EffectiveDate,
    decimal Rate,
    string Provider);
