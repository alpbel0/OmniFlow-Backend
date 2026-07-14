using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Infrastructure.BackgroundServices;

public sealed class TimeZoneBackfillWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<TimeZoneBackfillWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(6));
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
            var resolver = scope.ServiceProvider.GetRequiredService<ITimeZoneResolver>();
            var destinations = await context.TripDestinations
                .Where(x => x.Timezone == null && x.Latitude != null && x.Longitude != null)
                .Take(100)
                .ToListAsync(cancellationToken);
            foreach (var destination in destinations)
                destination.Timezone = resolver.Resolve(destination.Latitude, destination.Longitude);
            if (destinations.Count > 0)
                await context.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Timezone backfill batch failed");
        }
    }
}
