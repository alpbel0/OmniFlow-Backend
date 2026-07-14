using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OmniFlow.Application.Currency;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Settings;
using OmniFlow.Domain.Entities;
using OmniFlow.Infrastructure.Contexts;

namespace OmniFlow.Infrastructure.Services;

public sealed class FrankfurterExchangeRateService(
    HttpClient httpClient,
    ApplicationDbContext context,
    IOptions<CurrencySettings> options,
    IDateTimeService dateTimeService) : IExchangeRateService
{
    private readonly CurrencySettings _settings = options.Value;

    public async Task<ExchangeRateResult> GetRateAsync(
        string baseCurrency,
        string quoteCurrency,
        DateOnly? requestedDate,
        CancellationToken cancellationToken = default)
    {
        var normalizedBase = CurrencyPolicy.Normalize(baseCurrency);
        var normalizedQuote = CurrencyPolicy.Normalize(quoteCurrency);
        var date = requestedDate ?? DateOnly.FromDateTime(dateTimeService.NowUtc);
        if (normalizedBase == normalizedQuote)
            return new ExchangeRateResult(normalizedBase, normalizedQuote, date, date, 1m, _settings.Provider);

        var exact = await FindExactAsync(normalizedBase, normalizedQuote, date, cancellationToken);
        if (exact is not null)
            return ToResult(exact, date);

        try
        {
            var response = await FetchAsync(normalizedBase, normalizedQuote, date, cancellationToken);
            var snapshot = new ExchangeRateSnapshot
            {
                BaseCurrency = normalizedBase,
                QuoteCurrency = normalizedQuote,
                RateDate = response.Date,
                Rate = response.Rate,
                Provider = _settings.Provider,
                FetchedAtUtc = dateTimeService.NowUtc
            };
            context.ExchangeRateSnapshots.Add(snapshot);
            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                context.Entry(snapshot).State = EntityState.Detached;
                snapshot = await FindExactAsync(normalizedBase, normalizedQuote, response.Date, cancellationToken)
                    ?? throw new InvalidOperationException("The concurrent exchange-rate upsert could not be reloaded.");
            }
            return ToResult(snapshot, date);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            var fallback = await FindLatestPriorAsync(normalizedBase, normalizedQuote, date, cancellationToken);
            if (fallback is not null)
                return ToResult(fallback, date);
            throw new ApiException(
                "Exchange rate service is temporarily unavailable.",
                503,
                "EXCHANGE_RATE_UNAVAILABLE");
        }
    }

    private async Task<FrankfurterRateResponse> FetchAsync(
        string baseCurrency,
        string quoteCurrency,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var path = $"/v2/rate/{baseCurrency}/{quoteCurrency}?date={date:yyyy-MM-dd}&providers={Uri.EscapeDataString(_settings.Provider)}";
        var response = await httpClient.GetFromJsonAsync<FrankfurterRateResponse>(path, cancellationToken);
        if (response is null || response.Rate <= 0)
            throw new HttpRequestException("Frankfurter returned an invalid rate response.");
        return response;
    }

    private Task<ExchangeRateSnapshot?> FindExactAsync(string baseCurrency, string quoteCurrency, DateOnly date, CancellationToken cancellationToken) =>
        context.ExchangeRateSnapshots.FirstOrDefaultAsync(
            x => x.BaseCurrency == baseCurrency && x.QuoteCurrency == quoteCurrency &&
                 x.RateDate == date && x.Provider == _settings.Provider,
            cancellationToken);

    private Task<ExchangeRateSnapshot?> FindLatestPriorAsync(string baseCurrency, string quoteCurrency, DateOnly date, CancellationToken cancellationToken) =>
        context.ExchangeRateSnapshots
            .Where(x => x.BaseCurrency == baseCurrency && x.QuoteCurrency == quoteCurrency &&
                        x.RateDate <= date && x.Provider == _settings.Provider)
            .OrderByDescending(x => x.RateDate)
            .FirstOrDefaultAsync(cancellationToken);

    private static ExchangeRateResult ToResult(ExchangeRateSnapshot snapshot, DateOnly requestedDate) =>
        new(snapshot.BaseCurrency, snapshot.QuoteCurrency, requestedDate, snapshot.RateDate, snapshot.Rate, snapshot.Provider);

    private sealed record FrankfurterRateResponse(
        [property: JsonPropertyName("date")] DateOnly Date,
        [property: JsonPropertyName("rate")] decimal Rate);
}
