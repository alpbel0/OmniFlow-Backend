using Microsoft.EntityFrameworkCore;
using Moq;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Services;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.UnitTests.VisitLogs;

public sealed class VisitLogConversionServiceTests
{
    [Fact]
    public async Task MissingDestination_RecordsFailedAttemptForBackoffAndObservability()
    {
        var now = new DateTime(2026, 7, 14, 9, 0, 0, DateTimeKind.Utc);
        var context = new Mock<IApplicationDbContext>();
        var destinations = new Mock<DbSet<TripDestination>>();
        destinations.Setup(x => x.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TripDestination?)null);
        context.SetupGet(x => x.TripDestinations).Returns(destinations.Object);
        context.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var dateTimeService = new Mock<IDateTimeService>();
        dateTimeService.SetupGet(x => x.NowUtc).Returns(now);
        var service = new VisitLogConversionService(
            context.Object,
            Mock.Of<IExchangeRateService>(),
            Mock.Of<ITripTemporalService>(),
            dateTimeService.Object);
        var visitLog = PlaceVisitLog.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null,
            now.AddDays(-1), 10m, "EUR", 4, null, "USD");

        await service.TryCompleteAsync(visitLog, CancellationToken.None);

        visitLog.ConversionStatus.Should().Be(ConversionStatus.Pending);
        visitLog.ConversionAttemptCount.Should().Be(1);
        visitLog.LastConversionAttemptAtUtc.Should().Be(now);
        context.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
