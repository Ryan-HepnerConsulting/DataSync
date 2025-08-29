// DataSync.Functions.Tests/Infrastructure/ScheduleTests.cs

using DataSync.Functions.Helpers;
using FluentAssertions;
using Xunit;

namespace Tests.Infrastructure;

public class ScheduleTests
{
    [Xunit.Theory]
    [InlineData("0 0 * * * *",  "2025-01-01T00:10:00Z", "2025-01-01T01:00:00Z")] // hourly
    [InlineData("0 0 */2 * * *","2025-01-01T03:30:00Z", "2025-01-01T04:00:00Z")] // every 2h
    [InlineData("0 0 13 * * *", "2025-01-01T12:59:59Z", "2025-01-01T13:00:00Z")] // daily 13:00
    public void Computes_NextRunUtc(string cron, string nowUtc, string expectedUtc)
    {
        var now = DateTime.Parse(nowUtc, null, System.Globalization.DateTimeStyles.AdjustToUniversal);
        var next = CronUtil.GetNextUtc(cron, now);
        next.Should().Be(DateTime.Parse(expectedUtc).ToUniversalTime());
    }
}