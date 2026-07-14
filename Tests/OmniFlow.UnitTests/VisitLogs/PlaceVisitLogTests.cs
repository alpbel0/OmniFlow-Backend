using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.UnitTests.VisitLogs;

public class PlaceVisitLogTests
{
    [Fact]
    public void Create_WithBothTargets_ThrowsArgumentException()
    {
        var action = () => PlaceVisitLog.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow,
            null, "USD", null, null, "USD");

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNonUtcVisitedAt_ThrowsArgumentException()
    {
        var action = () => PlaceVisitLog.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), null, DateTime.Now,
            null, "USD", null, null, "USD");

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_NormalizesNoteAndCompletesSameCurrencyConversion()
    {
        var log = PlaceVisitLog.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), null, DateTime.UtcNow,
            12.34m, "usd", 5, "  excellent  ", "USD");

        log.Note.Should().Be("excellent");
        log.CurrencyCode.Should().Be("USD");
        log.ConversionStatus.Should().Be(ConversionStatus.Completed);
        log.ConvertedActualCost.Should().Be(12.34m);
        log.ExchangeRate.Should().Be(1m);
    }

    [Fact]
    public void Create_WithDifferentCurrency_MarksConversionPending()
    {
        var log = PlaceVisitLog.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            null, Guid.NewGuid(), DateTime.UtcNow,
            100m, "TRY", null, " ", "EUR");

        log.Note.Should().BeNull();
        log.ConversionStatus.Should().Be(ConversionStatus.Pending);
        log.ConvertedActualCost.Should().BeNull();
    }

    [Fact]
    public void Create_WithoutCost_MarksConversionNotRequired()
    {
        var log = PlaceVisitLog.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            null, Guid.NewGuid(), DateTime.UtcNow,
            null, "TRY", null, null, "EUR");

        log.ConversionStatus.Should().Be(ConversionStatus.NotRequired);
    }
}
