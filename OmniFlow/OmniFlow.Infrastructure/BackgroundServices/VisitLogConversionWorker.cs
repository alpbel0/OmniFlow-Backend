using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Services;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Infrastructure.BackgroundServices;

public sealed class VisitLogConversionWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<VisitLogConversionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        do
        {
            await ProcessBatchAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var conversion = scope.ServiceProvider.GetRequiredService<IVisitLogConversionService>();
            var now = scope.ServiceProvider.GetRequiredService<IDateTimeService>().NowUtc;
            var candidates = await context.PlaceVisitLogs
                .Where(x => x.ConversionStatus == ConversionStatus.Pending)
                .OrderBy(x => x.LastConversionAttemptAtUtc)
                .Take(100)
                .ToListAsync(cancellationToken);
            foreach (var log in candidates.Where(x => IsDue(x.ConversionAttemptCount, x.LastConversionAttemptAtUtc, now)))
                await conversion.TryCompleteAsync(log, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Visit-log conversion batch failed");
        }
    }

    private static bool IsDue(int attempts, DateTime? lastAttempt, DateTime now)
    {
        if (!lastAttempt.HasValue)
            return true;
        var exponent = Math.Min(Math.Max(attempts - 1, 0), 9);
        var delay = TimeSpan.FromMinutes(Math.Min(5 * Math.Pow(2, exponent), 24 * 60));
        return lastAttempt.Value.Add(delay) <= now;
    }
}
