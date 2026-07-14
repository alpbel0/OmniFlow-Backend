using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OmniFlow.Application.Currency;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Infrastructure.BackgroundServices;

public sealed class CurrencyRefreshWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<CurrencyRefreshWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RefreshAsync(stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(GetDelayUntilNextRun(), stoppingToken);
            await RefreshAsync(stoppingToken);
        }
    }

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IExchangeRateService>();
            foreach (var baseCurrency in CurrencyPolicy.Supported)
            foreach (var quoteCurrency in CurrencyPolicy.Supported)
                await service.GetRateAsync(baseCurrency, quoteCurrency, null, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Daily currency refresh failed");
        }
    }

    private static TimeSpan GetDelayUntilNextRun()
    {
        var now = DateTime.UtcNow;
        var next = now.Date.AddHours(18);
        if (next <= now)
            next = next.AddDays(1);
        return next - now;
    }
}
