using Moq;
using MyTracker.Domain.Interfaces;
using MyTracker.Domain.Services;

namespace MyTracker.Domain.Tests;

public class ActivityQueryServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);
    private readonly Mock<IActivityRepository> _repo = new();

    private ActivityQueryService CreateService() => new(_repo.Object, new FixedTimeProvider(Now));

    [Fact]
    public async Task GetActivityListAsync_MarksIsDownloaded_FromLocalCacheOnly()
    {
        _repo.Setup(r => r.GetCachedActivitiesAsync()).ReturnsAsync([TestData.Activity("1"), TestData.Activity("2")]);
        _repo.Setup(r => r.GetStoredActivityIdsAsync()).ReturnsAsync(["1"]);

        var list = await CreateService().GetActivityListAsync();

        Assert.True(list.Single(a => a.Id == "1").IsDownloaded);
        Assert.False(list.Single(a => a.Id == "2").IsDownloaded);
    }

    [Fact]
    public async Task GetHomeDashboardAsync_ComputesRollingWindowsAndPicksMostRecentActivity()
    {
        var day = Now.UtcDateTime;
        _repo.Setup(r => r.GetCachedActivitiesAsync()).ReturnsAsync(
        [
            TestData.Activity("old") with { Date = day.AddDays(-60), Distance = 10_000 },
            TestData.Activity("month") with { Date = day.AddDays(-20), Distance = 8_000 },
            TestData.Activity("week") with { Date = day.AddDays(-2), Distance = 5_000 },
        ]);
        _repo.Setup(r => r.GetStoredActivityIdsAsync()).ReturnsAsync(["week"]);

        var dashboard = await CreateService().GetHomeDashboardAsync();

        Assert.Equal(3, dashboard.TotalActivities);
        Assert.Equal(23.0, dashboard.TotalDistanceKm);
        Assert.Equal(5.0, dashboard.DistanceLast7DaysKm);
        Assert.Equal(13.0, dashboard.DistanceLast30DaysKm);
        Assert.Equal("week", dashboard.LatestActivity!.Id);
        Assert.True(dashboard.LatestActivityIsDownloaded);
    }

    [Fact]
    public async Task GetHomeDashboardAsync_HandlesEmptyCache()
    {
        _repo.Setup(r => r.GetCachedActivitiesAsync()).ReturnsAsync([]);
        _repo.Setup(r => r.GetStoredActivityIdsAsync()).ReturnsAsync([]);

        var dashboard = await CreateService().GetHomeDashboardAsync();

        Assert.Equal(0, dashboard.TotalActivities);
        Assert.Null(dashboard.LatestActivity);
        Assert.False(dashboard.LatestActivityIsDownloaded);
    }
}
