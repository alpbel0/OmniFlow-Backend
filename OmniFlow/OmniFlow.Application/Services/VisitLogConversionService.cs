using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Services;

public interface IVisitLogConversionService
{
    Task TryCompleteAsync(PlaceVisitLog visitLog, CancellationToken cancellationToken);
}

public sealed class VisitLogConversionService(
    IApplicationDbContext context,
    IExchangeRateService exchangeRateService,
    ITripTemporalService temporalService,
    IDateTimeService dateTimeService) : IVisitLogConversionService
{
    public async Task TryCompleteAsync(PlaceVisitLog visitLog, CancellationToken cancellationToken)
    {
        if (visitLog.ConversionStatus != ConversionStatus.Pending || !visitLog.ActualCost.HasValue)
            return;

        try
        {
            var destination = await context.TripDestinations.FindAsync([visitLog.TripDestinationId], cancellationToken);
            if (string.IsNullOrWhiteSpace(destination?.Timezone))
            {
                visitLog.RecordConversionFailure(dateTimeService.NowUtc);
                await context.SaveChangesAsync(cancellationToken);
                return;
            }
            var requestedDate = temporalService.GetLocalDate(visitLog.VisitedAt, destination.Timezone);
            var rate = await exchangeRateService.GetRateAsync(
                visitLog.CurrencyCode,
                visitLog.BaseCurrencyCode,
                requestedDate,
                cancellationToken);
            visitLog.CompleteConversion(
                visitLog.ActualCost.Value * rate.Rate,
                rate.Rate,
                requestedDate,
                rate.EffectiveDate);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            visitLog.RecordConversionFailure(dateTimeService.NowUtc);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
