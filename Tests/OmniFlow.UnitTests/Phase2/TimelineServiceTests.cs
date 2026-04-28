using FluentAssertions;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;
using OmniFlow.Infrastructure.Services;

namespace OmniFlow.UnitTests.Phase2;

public class TimelineServiceTests
{
    private readonly TimelineService _sut = new();

    #region GetDailyCapacity

    [Theory]
    [InlineData(Tempo.Slow, 3)]
    [InlineData(Tempo.Moderate, 5)]
    [InlineData(Tempo.Fast, 7)]
    public void GetDailyCapacity_ReturnsExpected(Tempo tempo, int expected)
    {
        _sut.GetDailyCapacity(tempo).Should().Be(expected);
    }

    [Fact]
    public void GetDailyCapacity_UnknownDefaultsToFive()
    {
        _sut.GetDailyCapacity((Tempo)999).Should().Be(5);
    }

    #endregion

    #region GetTimeRange

    [Fact]
    public void GetTimeRange_Place_ReturnsCorrectRange()
    {
        var entry = TimelineEntry.CreatePlaceEntry(
            Guid.NewGuid(), Guid.NewGuid(), 1, 100, Guid.NewGuid());
        entry.StartTime = new TimeOnly(10, 0);
        entry.DurationMinutes = 90;

        var result = _sut.GetTimeRange(entry, new DateOnly(2026, 8, 10));

        result.Should().NotBeNull();
        result!.Value.Start.Should().Be(new DateTime(2026, 8, 10, 10, 0, 0));
        result.Value.End.Should().Be(new DateTime(2026, 8, 10, 11, 30, 0));
    }

    [Fact]
    public void GetTimeRange_Place_DifferentDayNumber()
    {
        var entry = TimelineEntry.CreatePlaceEntry(
            Guid.NewGuid(), Guid.NewGuid(), 3, 100, Guid.NewGuid());
        entry.StartTime = new TimeOnly(14, 0);
        entry.DurationMinutes = 60;

        var result = _sut.GetTimeRange(entry, new DateOnly(2026, 8, 10));

        result.Should().NotBeNull();
        result!.Value.Start.Should().Be(new DateTime(2026, 8, 12, 14, 0, 0));
    }

    [Fact]
    public void GetTimeRange_CustomEvent_ReturnsCorrectRange()
    {
        var entry = TimelineEntry.CreateCustomEventEntry(
            Guid.NewGuid(), Guid.NewGuid(), 1, 100,
            "Concert", new TimeOnly(20, 0), 180);

        var result = _sut.GetTimeRange(entry, new DateOnly(2026, 8, 10));

        result.Should().NotBeNull();
        result!.Value.Start.Should().Be(new DateTime(2026, 8, 10, 20, 0, 0));
        result.Value.End.Should().Be(new DateTime(2026, 8, 10, 23, 0, 0));
    }

    [Fact]
    public void GetTimeRange_CustomFlight_WithBuffer()
    {
        var departure = new DateTime(2026, 8, 10, 10, 0, 0);
        var arrival = new DateTime(2026, 8, 10, 12, 0, 0);
        var entry = TimelineEntry.CreateCustomFlightEntry(
            Guid.NewGuid(), Guid.NewGuid(), 1, 100,
            "IST", "FCO", departure, arrival);

        var result = _sut.GetTimeRange(entry, new DateOnly(2026, 8, 10));

        result.Should().NotBeNull();
        result!.Value.Start.Should().Be(new DateTime(2026, 8, 10, 8, 0, 0)); // 10:00 - 120min
        result.Value.End.Should().Be(new DateTime(2026, 8, 10, 12, 0, 0));
    }

    [Fact]
    public void GetTimeRange_CustomTransport_WithBuffer()
    {
        var entry = TimelineEntry.CreateCustomTransportEntry(
            Guid.NewGuid(), Guid.NewGuid(), 1, 100,
            TransportMode.Train,
            new TimeOnly(14, 0), 120, // startTime, durationMinutes
            "Roma Termini", "Firenze SMN", "Trenitalia");

        var result = _sut.GetTimeRange(entry, new DateOnly(2026, 8, 10));

        result.Should().NotBeNull();
        result!.Value.Start.Should().Be(new DateTime(2026, 8, 10, 13, 30, 0)); // 14:00 - 30min
        result.Value.End.Should().Be(new DateTime(2026, 8, 10, 16, 0, 0));   // 14:00 + 120min
    }

    [Fact]
    public void GetTimeRange_CustomAccommodation_ReturnsNull()
    {
        var entry = TimelineEntry.CreateCustomAccommodationEntry(
            Guid.NewGuid(), Guid.NewGuid(), 1, 100,
            new DateTime(2026, 8, 10, 14, 0, 0),
            new DateTime(2026, 8, 13, 12, 0, 0),
            "Hotel Artis");

        var result = _sut.GetTimeRange(entry, new DateOnly(2026, 8, 10));

        result.Should().BeNull();
    }

    [Fact]
    public void GetTimeRange_MissingTimeFields_ReturnsNull()
    {
        var entry = TimelineEntry.CreatePlaceEntry(
            Guid.NewGuid(), Guid.NewGuid(), 1, 100, Guid.NewGuid());
        // StartTime and DurationMinutes not set

        var result = _sut.GetTimeRange(entry, new DateOnly(2026, 8, 10));

        result.Should().BeNull();
    }

    #endregion

    #region CheckConflict

    [Fact]
    public void CheckConflict_NoOverlap_ReturnsValid()
    {
        var existing = CreatePlace("09:00", 60);
        var newEntry = CreatePlace("11:00", 60);

        var result = _sut.CheckConflict(newEntry, [existing], new DateOnly(2026, 8, 10));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CheckConflict_PlaceOverlapsLockedFlight_ReturnsInvalid()
    {
        var flight = TimelineEntry.CreateCustomFlightEntry(
            Guid.NewGuid(), Guid.NewGuid(), 1, 100,
            "IST", "FCO",
            new DateTime(2026, 8, 10, 10, 0, 0),
            new DateTime(2026, 8, 10, 12, 0, 0));
        flight.Id = Guid.NewGuid();

        var place = CreatePlace("09:30", 60); // overlaps with flight buffer 08:00-12:00
        place.Id = Guid.NewGuid();

        var result = _sut.CheckConflict(place, [flight], new DateOnly(2026, 8, 10));

        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be("CONFLICT");
    }

    [Fact]
    public void CheckConflict_TwoUnlockedPlaces_Overlap_ReturnsValid()
    {
        // Both are unlocked Place entries — user can freely arrange them
        var existing = CreatePlace("10:00", 60);
        existing.Id = Guid.NewGuid();

        var newEntry = CreatePlace("10:30", 60);
        newEntry.Id = Guid.NewGuid();

        var result = _sut.CheckConflict(newEntry, [existing], new DateOnly(2026, 8, 10));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CheckConflict_LockedEventOverlapsPlace_ReturnsInvalid()
    {
        var place = CreatePlace("10:00", 120);
        place.Id = Guid.NewGuid();

        var ev = TimelineEntry.CreateCustomEventEntry(
            Guid.NewGuid(), Guid.NewGuid(), 1, 100,
            "Meeting", new TimeOnly(10, 30), 60);
        ev.Id = Guid.NewGuid();

        var result = _sut.CheckConflict(ev, [place], new DateOnly(2026, 8, 10));

        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be("CONFLICT");
    }

    [Fact]
    public void CheckConflict_TransportBufferOverlaps_ReturnsInvalid()
    {
        var transport = TimelineEntry.CreateCustomTransportEntry(
            Guid.NewGuid(), Guid.NewGuid(), 1, 100,
            TransportMode.Train,
            new TimeOnly(14, 0), 120,
            "Roma", "Firenze");
        transport.Id = Guid.NewGuid();

        var place = CreatePlace("13:00", 120); // overlaps with transport 13:30-16:00
        place.Id = Guid.NewGuid();

        var result = _sut.CheckConflict(place, [transport], new DateOnly(2026, 8, 10));

        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be("CONFLICT");
    }

    [Fact]
    public void CheckConflict_SameEntrySkipped_ReturnsValid()
    {
        var entry = CreatePlace("10:00", 60);
        entry.Id = Guid.NewGuid();

        var result = _sut.CheckConflict(entry, [entry], new DateOnly(2026, 8, 10));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CheckConflict_CustomAccommodationIgnored()
    {
        var acc = TimelineEntry.CreateCustomAccommodationEntry(
            Guid.NewGuid(), Guid.NewGuid(), 1, 100,
            new DateTime(2026, 8, 10, 14, 0, 0),
            new DateTime(2026, 8, 13, 12, 0, 0),
            "Hotel");
        acc.Id = Guid.NewGuid();

        var place = CreatePlace("10:00", 60);
        place.Id = Guid.NewGuid();

        var result = _sut.CheckConflict(place, [acc], new DateOnly(2026, 8, 10));

        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region ValidateNewEntry

    [Fact]
    public void ValidateNewEntry_CapacityOk_ReturnsValid()
    {
        var dayEntries = new List<TimelineEntry>
        {
            CreatePlaceWithId("09:00", 60),
            CreatePlaceWithId("11:00", 60),
        };
        var newEntry = CreatePlace("14:00", 60);

        var result = _sut.ValidateNewEntry(newEntry, dayEntries, Tempo.Slow, new DateOnly(2026, 8, 10));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateNewEntry_CapacityExceeded_ReturnsInvalid()
    {
        var dayEntries = new List<TimelineEntry>
        {
            CreatePlaceWithId("09:00", 60),
            CreatePlaceWithId("11:00", 60),
            CreatePlaceWithId("13:00", 60),
        };
        var newEntry = CreatePlace("15:00", 60);

        var result = _sut.ValidateNewEntry(newEntry, dayEntries, Tempo.Slow, new DateOnly(2026, 8, 10));

        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be("CAPACITY_EXCEEDED");
    }

    [Fact]
    public void ValidateNewEntry_CustomEventCountsTowardsCapacity()
    {
        var dayEntries = new List<TimelineEntry>
        {
            CreatePlaceWithId("09:00", 60),
            CreatePlaceWithId("11:00", 60),
            CreatePlaceWithId("13:00", 60),
        };
        var newEntry = TimelineEntry.CreateCustomEventEntry(
            Guid.NewGuid(), Guid.NewGuid(), 1, 100,
            "Concert", new TimeOnly(20, 0), 120);

        var result = _sut.ValidateNewEntry(newEntry, dayEntries, Tempo.Slow, new DateOnly(2026, 8, 10));

        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be("CAPACITY_EXCEEDED");
    }

    [Fact]
    public void ValidateNewEntry_FlightIgnoredInCapacity()
    {
        var flight = TimelineEntry.CreateCustomFlightEntry(
            Guid.NewGuid(), Guid.NewGuid(), 1, 100,
            "IST", "FCO",
            new DateTime(2026, 8, 10, 10, 0, 0),
            new DateTime(2026, 8, 10, 12, 0, 0));
        flight.Id = Guid.NewGuid();

        var dayEntries = new List<TimelineEntry>
        {
            CreatePlaceWithId("09:00", 60),
            CreatePlaceWithId("14:00", 60),
            flight,
        };
        var newEntry = CreatePlace("16:00", 60);

        var result = _sut.ValidateNewEntry(newEntry, dayEntries, Tempo.Slow, new DateOnly(2026, 8, 10));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateNewEntry_TransportIgnoredInCapacity()
    {
        var transport = TimelineEntry.CreateCustomTransportEntry(
            Guid.NewGuid(), Guid.NewGuid(), 1, 100,
            TransportMode.Bus,
            new TimeOnly(10, 0), 120);
        transport.Id = Guid.NewGuid();

        var dayEntries = new List<TimelineEntry>
        {
            CreatePlaceWithId("09:00", 60),
            CreatePlaceWithId("14:00", 60),
            transport,
        };
        var newEntry = CreatePlace("16:00", 60);

        var result = _sut.ValidateNewEntry(newEntry, dayEntries, Tempo.Slow, new DateOnly(2026, 8, 10));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateNewEntry_AccommodationIgnoredInCapacity()
    {
        var acc = TimelineEntry.CreateCustomAccommodationEntry(
            Guid.NewGuid(), Guid.NewGuid(), 1, 100,
            new DateTime(2026, 8, 10, 14, 0, 0),
            new DateTime(2026, 8, 13, 12, 0, 0),
            "Hotel");
        acc.Id = Guid.NewGuid();

        var dayEntries = new List<TimelineEntry>
        {
            CreatePlaceWithId("09:00", 60),
            CreatePlaceWithId("14:00", 60),
            acc,
        };
        var newEntry = CreatePlace("16:00", 60);

        var result = _sut.ValidateNewEntry(newEntry, dayEntries, Tempo.Slow, new DateOnly(2026, 8, 10));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateNewEntry_ConflictTakesPrecedenceOverCapacity()
    {
        // One entry only, but it conflicts — conflict should be reported first
        var flight = TimelineEntry.CreateCustomFlightEntry(
            Guid.NewGuid(), Guid.NewGuid(), 1, 100,
            "IST", "FCO",
            new DateTime(2026, 8, 10, 10, 0, 0),
            new DateTime(2026, 8, 10, 12, 0, 0));
        flight.Id = Guid.NewGuid();

        var dayEntries = new List<TimelineEntry> { flight };
        var place = CreatePlace("09:30", 60);

        var result = _sut.ValidateNewEntry(place, dayEntries, Tempo.Slow, new DateOnly(2026, 8, 10));

        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be("CONFLICT");
    }

    #endregion

    #region GetLexoRankBetween

    [Fact]
    public void GetLexoRankBetween_First_ReturnsStep()
    {
        _sut.GetLexoRankBetween(null, 1000).Should().Be(500);
    }

    [Fact]
    public void GetLexoRankBetween_Last_ReturnsStep()
    {
        _sut.GetLexoRankBetween(1000, null).Should().Be(1500);
    }

    [Fact]
    public void GetLexoRankBetween_Middle_ReturnsAverage()
    {
        _sut.GetLexoRankBetween(1000, 2000).Should().Be(1500);
    }

    [Fact]
    public void GetLexoRankBetween_BothNull_ReturnsDefault()
    {
        _sut.GetLexoRankBetween(null, null).Should().Be(500);
    }

    [Fact]
    public void GetLexoRankBetween_Fractional()
    {
        _sut.GetLexoRankBetween(1000, 1001).Should().Be(1000.5);
    }

    #endregion

    #region Helpers

    private static TimelineEntry CreatePlace(string time, int durationMinutes)
    {
        var entry = TimelineEntry.CreatePlaceEntry(
            Guid.NewGuid(), Guid.NewGuid(), 1, 100, Guid.NewGuid());
        entry.StartTime = TimeOnly.Parse(time);
        entry.DurationMinutes = durationMinutes;
        return entry;
    }

    private static TimelineEntry CreatePlaceWithId(string time, int durationMinutes)
    {
        var entry = CreatePlace(time, durationMinutes);
        entry.Id = Guid.NewGuid();
        return entry;
    }

    #endregion
}
