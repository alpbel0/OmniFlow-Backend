using OmniFlow.Domain.Common;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Domain.Entities;

public class PlaceVisitLog : AuditableBaseEntity
{
    public Guid TripId { get; private set; }
    public Guid TripDestinationId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? TimelineEntryId { get; private set; }
    public Guid? PlaceId { get; private set; }
    public decimal? ActualCost { get; private set; }
    public string CurrencyCode { get; private set; } = "USD";
    public int? Rating { get; private set; }
    public string? Note { get; private set; }
    public DateTime VisitedAt { get; private set; }
    public decimal? ConvertedActualCost { get; private set; }
    public decimal? ExchangeRate { get; private set; }
    public DateOnly? RateRequestedDate { get; private set; }
    public DateOnly? ExchangeRateDate { get; private set; }
    public string BaseCurrencyCode { get; private set; } = "USD";
    public ConversionStatus ConversionStatus { get; private set; }
    public int ConversionAttemptCount { get; private set; }
    public DateTime? LastConversionAttemptAtUtc { get; private set; }

    public Trip Trip { get; private set; } = null!;
    public TripDestination TripDestination { get; private set; } = null!;
    public User User { get; private set; } = null!;
    public TimelineEntry? TimelineEntry { get; private set; }
    public Place? Place { get; private set; }

    private PlaceVisitLog()
    {
    }

    public static PlaceVisitLog Create(
        Guid tripId,
        Guid tripDestinationId,
        Guid userId,
        Guid? timelineEntryId,
        Guid? placeId,
        DateTime visitedAt,
        decimal? actualCost,
        string currencyCode,
        int? rating,
        string? note,
        string baseCurrencyCode)
    {
        ValidateTargets(timelineEntryId, placeId);
        ValidateMutableValues(visitedAt, actualCost, rating, note);

        var log = new PlaceVisitLog
        {
            TripId = tripId,
            TripDestinationId = tripDestinationId,
            UserId = userId,
            TimelineEntryId = timelineEntryId,
            PlaceId = placeId,
            VisitedAt = visitedAt,
            ActualCost = actualCost,
            CurrencyCode = NormalizeCurrency(currencyCode),
            Rating = rating,
            Note = NormalizeNote(note),
            BaseCurrencyCode = NormalizeCurrency(baseCurrencyCode)
        };
        log.ResetConversion();
        return log;
    }

    public void Update(
        DateTime visitedAt,
        decimal? actualCost,
        string currencyCode,
        int? rating,
        string? note,
        Guid? spontaneousDestinationId = null)
    {
        ValidateMutableValues(visitedAt, actualCost, rating, note);
        VisitedAt = visitedAt;
        ActualCost = actualCost;
        CurrencyCode = NormalizeCurrency(currencyCode);
        Rating = rating;
        Note = NormalizeNote(note);
        if (TimelineEntryId is null && spontaneousDestinationId.HasValue)
            TripDestinationId = spontaneousDestinationId.Value;
        ResetConversion();
    }

    public void CompleteConversion(decimal convertedAmount, decimal rate, DateOnly requestedDate, DateOnly rateDate)
    {
        if (rate <= 0)
            throw new ArgumentOutOfRangeException(nameof(rate));

        ConvertedActualCost = decimal.Round(convertedAmount, 2, MidpointRounding.AwayFromZero);
        ExchangeRate = decimal.Round(rate, 8, MidpointRounding.AwayFromZero);
        RateRequestedDate = requestedDate;
        ExchangeRateDate = rateDate;
        ConversionStatus = ConversionStatus.Completed;
        LastConversionAttemptAtUtc = DateTime.UtcNow;
    }

    public void RecordConversionFailure(DateTime attemptedAtUtc)
    {
        ConversionAttemptCount++;
        LastConversionAttemptAtUtc = attemptedAtUtc;
        ConversionStatus = ConversionStatus.Pending;
    }

    private void ResetConversion()
    {
        ConvertedActualCost = null;
        ExchangeRate = null;
        RateRequestedDate = null;
        ExchangeRateDate = null;
        ConversionAttemptCount = 0;
        LastConversionAttemptAtUtc = null;

        if (!ActualCost.HasValue)
        {
            ConversionStatus = ConversionStatus.NotRequired;
            return;
        }

        if (CurrencyCode == BaseCurrencyCode)
        {
            ConvertedActualCost = ActualCost.Value;
            ExchangeRate = 1m;
            ConversionStatus = ConversionStatus.Completed;
            return;
        }

        ConversionStatus = ConversionStatus.Pending;
    }

    private static void ValidateTargets(Guid? timelineEntryId, Guid? placeId)
    {
        if (timelineEntryId.HasValue == placeId.HasValue)
            throw new ArgumentException("Exactly one visit target is required.");
    }

    private static void ValidateMutableValues(DateTime visitedAt, decimal? actualCost, int? rating, string? note)
    {
        if (visitedAt.Kind != DateTimeKind.Utc)
            throw new ArgumentException("VisitedAt must be UTC.", nameof(visitedAt));
        if (actualCost < 0)
            throw new ArgumentOutOfRangeException(nameof(actualCost));
        if (rating is < 1 or > 5)
            throw new ArgumentOutOfRangeException(nameof(rating));
        if (note?.Trim().Length > 1000)
            throw new ArgumentException("Note cannot exceed 1000 characters.", nameof(note));
    }

    private static string NormalizeCurrency(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        var normalized = code.Trim().ToUpperInvariant();
        if (normalized.Length != 3)
            throw new ArgumentException("Currency code must contain three characters.", nameof(code));
        return normalized;
    }

    private static string? NormalizeNote(string? note) =>
        string.IsNullOrWhiteSpace(note) ? null : note.Trim();
}
