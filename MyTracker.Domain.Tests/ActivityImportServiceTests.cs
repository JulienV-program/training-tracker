using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MyTracker.Domain.Exceptions;
using MyTracker.Domain.Interfaces;
using MyTracker.Domain.Models;
using MyTracker.Domain.Services;

namespace MyTracker.Domain.Tests;

public class ActivityImportServiceTests
{
    private readonly Mock<IActivityProvider> _provider = new();
    private readonly Mock<IActivityRepository> _repo = new();

    public ActivityImportServiceTests()
        => _provider.Setup(p => p.GetValidAccessTokenAsync()).ReturnsAsync("token");

    private ActivityImportService CreateService()
        => new(_provider.Object, _repo.Object, NullLogger<ActivityImportService>.Instance);

    private void SetupStravaDetailAndStreams(string id, List<ActivityDataPoint> streams, ActivityDetail? detail = null)
    {
        _provider.Setup(p => p.GetActivityDetailAsync(id, "token")).ReturnsAsync(detail ?? TestData.Detail(id));
        _provider.Setup(p => p.GetActivityStreamsAsync(id, "token")).ReturnsAsync(streams);
    }

    [Fact]
    public async Task GetOrImportDataPointsAsync_ReturnsCache_WhenAlreadyImported()
    {
        _repo.Setup(r => r.ExistsAsync("123")).ReturnsAsync(true);
        var cachedPoints = TestData.Points();
        _repo.Setup(r => r.GetDataPointsAsync("123")).ReturnsAsync(cachedPoints);

        var result = await CreateService().GetOrImportDataPointsAsync("123");

        Assert.Same(cachedPoints, result);
        _provider.Verify(p => p.GetValidAccessTokenAsync(), Times.Never);
    }

    [Fact]
    public async Task GetOrImportDataPointsAsync_ImportsAndSaves_UsingCachedSummary_WhenNotImported()
    {
        var activity = TestData.Activity();
        _repo.Setup(r => r.ExistsAsync("123")).ReturnsAsync(false);
        _repo.Setup(r => r.GetActivityAsync("123")).ReturnsAsync(activity); // résumé issu de la synchro de liste
        var detail = TestData.Detail();
        var streams = TestData.Points();
        SetupStravaDetailAndStreams("123", streams, detail);

        var result = await CreateService().GetOrImportDataPointsAsync("123");

        Assert.Equal(streams, result);
        _provider.Verify(p => p.GetActivitiesAsync(It.IsAny<string>()), Times.Never); // pas de re-pagination
        _repo.Verify(r => r.SaveActivityAsync(activity), Times.Once);
        _repo.Verify(r => r.SaveActivityDetailAsync(detail), Times.Once);
        _repo.Verify(r => r.SaveDataPointsAsync("123", It.Is<IEnumerable<ActivityDataPoint>>(p => p.SequenceEqual(streams))), Times.Once);
    }

    [Fact]
    public async Task GetOrImportDataPointsAsync_FallsBackToStravaList_WhenSummaryNotCached()
    {
        var activity = TestData.Activity();
        _provider.Setup(p => p.GetActivitiesAsync("token")).ReturnsAsync([activity]);
        SetupStravaDetailAndStreams("123", TestData.Points());

        await CreateService().GetOrImportDataPointsAsync("123");

        _repo.Verify(r => r.SaveActivityAsync(activity), Times.Once);
    }

    [Fact]
    public async Task GetOrImportDataPointsAsync_ForceReimport_BypassesCacheAndRefreshesSummary()
    {
        _repo.Setup(r => r.ExistsAsync("123")).ReturnsAsync(true);
        _repo.Setup(r => r.GetActivityAsync("123")).ReturnsAsync(TestData.Activity() with { Name = "ancien nom" });
        var fresh = TestData.Activity() with { Name = "nom à jour" };
        _provider.Setup(p => p.GetActivitiesAsync("token")).ReturnsAsync([fresh]);
        SetupStravaDetailAndStreams("123", []);

        await CreateService().GetOrImportDataPointsAsync("123", forceReimport: true);

        _repo.Verify(r => r.GetDataPointsAsync(It.IsAny<string>()), Times.Never);
        _repo.Verify(r => r.SaveActivityAsync(fresh), Times.Once);
    }

    [Fact]
    public async Task GetOrImportDataPointsAsync_ThrowsActivityNotFound_WhenUnknownEverywhere()
    {
        _provider.Setup(p => p.GetActivitiesAsync("token")).ReturnsAsync([TestData.Activity("123")]);

        var ex = await Assert.ThrowsAsync<ActivityNotFoundException>(() => CreateService().GetOrImportDataPointsAsync("999"));

        Assert.Equal("999", ex.ActivityId);
    }

    [Fact]
    public async Task SyncActivitiesListAsync_FetchesFromStravaAndSavesSummaries()
    {
        var activities = new List<Activity> { TestData.Activity("1"), TestData.Activity("2") };
        _provider.Setup(p => p.GetActivitiesAsync("token")).ReturnsAsync(activities);

        await CreateService().SyncActivitiesListAsync();

        _repo.Verify(r => r.SaveActivitySummariesAsync(It.Is<IEnumerable<Activity>>(a => a.SequenceEqual(activities))), Times.Once);
    }

    [Fact]
    public async Task GetLatestStravaActivityIdAsync_PicksMostRecentByDate_NotListOrder()
    {
        var older = TestData.Activity("1") with { Date = DateTime.UtcNow.AddDays(-3) };
        var newer = TestData.Activity("2") with { Date = DateTime.UtcNow };
        _provider.Setup(p => p.GetActivitiesAsync("token")).ReturnsAsync([older, newer]);

        Assert.Equal("2", await CreateService().GetLatestStravaActivityIdAsync());
    }
}
