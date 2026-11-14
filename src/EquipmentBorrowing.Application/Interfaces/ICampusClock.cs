namespace EquipmentBorrowing.Application.Interfaces;

public interface ICampusClock
{
    DateOnly Today { get; }
}

public sealed class CampusClock : ICampusClock
{
    private readonly TimeProvider _timeProvider;
    private readonly TimeZoneInfo _timeZone;

    public CampusClock(TimeProvider? timeProvider = null, TimeZoneInfo? timeZone = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        _timeZone = timeZone ?? TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");
    }

    public DateOnly Today => DateOnly.FromDateTime(
        TimeZoneInfo.ConvertTime(_timeProvider.GetUtcNow(), _timeZone).DateTime);
}
