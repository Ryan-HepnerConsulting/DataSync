using Cronos;

namespace DataSync.Functions.Helpers;

public static class CronUtil
{
    /// <summary>Compute the next UTC occurrence for a 6-field cron (sec min hour day month dow).</summary>
    public static DateTime GetNextUtc(string cron, DateTime nowUtc)
    {
        var expr = CronExpression.Parse(cron, CronFormat.IncludeSeconds);
        return expr.GetNextOccurrence(nowUtc, TimeZoneInfo.Utc)?.ToUniversalTime()
               ?? nowUtc.AddHours(1);
    }
}