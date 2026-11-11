namespace CarServices.Application.Common.Time;

public static class EgyptTime
{
    public static readonly TimeZoneInfo TimeZone = ResolveCairoTimeZone();

    private static TimeZoneInfo ResolveCairoTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");
        }
        catch
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
        }
    }

    public static DateTime UtcNow() => DateTime.UtcNow;

    public static DateTime NowInCairo() =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZone);

    public static DateOnly TodayInCairo() =>
        DateOnly.FromDateTime(NowInCairo());

    public static DateTime ToUtc(DateOnly date, TimeOnly time)
    {
        var unspecified = date.ToDateTime(time, DateTimeKind.Unspecified);

        // Handle rare DST spring-forward invalid time or fall-back ambiguous time gracefully
        if (TimeZone.IsInvalidTime(unspecified))
        {
            unspecified = unspecified.AddHours(1);
        }

        var offset = TimeZone.GetUtcOffset(unspecified);
        return new DateTimeOffset(unspecified, offset).UtcDateTime;
    }

    public static DateTime ToCairo(DateTime utcDateTime)
    {
        var normalizedUtc = utcDateTime.Kind == DateTimeKind.Utc
            ? utcDateTime
            : DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);

        return TimeZoneInfo.ConvertTimeFromUtc(normalizedUtc, TimeZone);
    }
}

