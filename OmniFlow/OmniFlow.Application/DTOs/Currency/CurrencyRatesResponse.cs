namespace OmniFlow.Application.DTOs.Currency;

public sealed record CurrencyRateItem(
    string QuoteCurrencyCode,
    decimal Rate,
    DateOnly RequestedDate,
    DateOnly EffectiveDate,
    string Provider);

public sealed record CurrencyRatesResponse(
    string BaseCurrencyCode,
    IReadOnlyList<CurrencyRateItem> Rates);
