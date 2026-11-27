namespace Synq.Common.Time;

public static class TimeProviderExtensions
{
    public static DateTime UtcNowMs(this TimeProvider time)
    {
        var now = time.GetUtcNow().UtcDateTime;
        return new DateTime(now.Ticks - now.Ticks % TimeSpan.TicksPerMillisecond, DateTimeKind.Utc);
    }
}
