using Moq;
using MyTracker.Domain.Configurations;
using MyTracker.Domain.Interfaces;
using MyTracker.Domain.Models;
using MyTracker.Domain.Services;
using Xunit;

namespace MyTracker.Domain.Tests;

public class ActivityServiceTests
{
    private readonly Mock<IActivityProvider> _provider = new();
    private readonly Mock<IActivityRepository> _repo = new();
    private readonly Mock<ICsvExportService> _csvExportService = new();
    private readonly Mock<IActivityCommentaryRepository> _commentaryRepo = new();
    private readonly Mock<IOllamaService> _ollama = new();
    private readonly OllamaSettings _ollamaSettings = new() { Model = "test-model" };

    private ActivityService CreateService() => new(
        _provider.Object, _repo.Object, _csvExportService.Object, _commentaryRepo.Object, _ollama.Object, _ollamaSettings);

    private static Activity MakeActivity(string id = "123") => new(
        id, "Test Run", DateTime.UtcNow, "Run", 5000, 1800, 50,
        150, 180, null, null, 80, 300, 60);

    [Fact]
    public async Task GetActivityDataPointsAsync_ReturnsCache_WhenExistsAndNotForcingReimport()
    {
        _repo.Setup(r => r.ExistsAsync("123")).ReturnsAsync(true);
        var cachedPoints = new List<ActivityDataPoint> { new(0, 0, 100, null, null, null, null, null) };
        _repo.Setup(r => r.GetDataPointsAsync("123")).ReturnsAsync(cachedPoints);

        var service = CreateService();
        var result = await service.GetActivityDataPointsAsync("123");

        Assert.Same(cachedPoints, result);
        _provider.Verify(p => p.GetActivitiesAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetActivityDataPointsAsync_FetchesAndSaves_WhenNotCached()
    {
        _repo.Setup(r => r.ExistsAsync("123")).ReturnsAsync(false);
        _provider.Setup(p => p.GetValidAccessTokenAsync()).ReturnsAsync("token");
        var activity = MakeActivity();
        _provider.Setup(p => p.GetActivitiesAsync("token")).ReturnsAsync([activity]);
        var detail = new ActivityDetail("123", 3.0, 5.0, null, null, null, null, null, [], []);
        _provider.Setup(p => p.GetActivityDetailAsync("123", "token")).ReturnsAsync(detail);
        var streams = new List<ActivityDataPoint> { new(0, 0, 100, null, null, null, null, null) };
        _provider.Setup(p => p.GetActivityStreamsAsync("123", "token")).ReturnsAsync(streams);

        var service = CreateService();
        var result = await service.GetActivityDataPointsAsync("123");

        Assert.Same(streams, result);
        _repo.Verify(r => r.SaveActivityAsync(activity), Times.Once);
        _repo.Verify(r => r.SaveActivityDetailAsync(detail), Times.Once);
        _repo.Verify(r => r.SaveDataPointsAsync("123", streams), Times.Once);
    }

    [Fact]
    public async Task GetActivityDataPointsAsync_ForceReimport_BypassesCacheEvenIfExists()
    {
        _repo.Setup(r => r.ExistsAsync("123")).ReturnsAsync(true);
        _provider.Setup(p => p.GetValidAccessTokenAsync()).ReturnsAsync("token");
        var activity = MakeActivity();
        _provider.Setup(p => p.GetActivitiesAsync("token")).ReturnsAsync([activity]);
        _provider.Setup(p => p.GetActivityDetailAsync("123", "token"))
            .ReturnsAsync(new ActivityDetail("123", 3.0, 5.0, null, null, null, null, null, [], []));
        _provider.Setup(p => p.GetActivityStreamsAsync("123", "token")).ReturnsAsync([]);

        var service = CreateService();
        await service.GetActivityDataPointsAsync("123", forceReimport: true);

        _repo.Verify(r => r.GetDataPointsAsync(It.IsAny<string>()), Times.Never);
        _provider.Verify(p => p.GetActivitiesAsync("token"), Times.Once);
    }

    [Fact]
    public async Task GetActivityDataPointsAsync_Throws_WhenActivityNotFoundInStravaList()
    {
        _repo.Setup(r => r.ExistsAsync("999")).ReturnsAsync(false);
        _provider.Setup(p => p.GetValidAccessTokenAsync()).ReturnsAsync("token");
        _provider.Setup(p => p.GetActivitiesAsync("token")).ReturnsAsync([MakeActivity("123")]);

        var service = CreateService();
        await Assert.ThrowsAsync<Exception>(() => service.GetActivityDataPointsAsync("999"));
    }

    [Fact]
    public async Task GetOrGenerateCommentaryAsync_ReturnsCached_WhenPresent()
    {
        _commentaryRepo.Setup(c => c.GetCommentaryAsync("123")).ReturnsAsync("commentaire en cache");

        var service = CreateService();
        var result = await service.GetOrGenerateCommentaryAsync("123");

        Assert.Equal("commentaire en cache", result);
        _ollama.Verify(o => o.GenerateCommentaryAsync(It.IsAny<Activity>(), It.IsAny<IEnumerable<ActivityDataPoint>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetOrGenerateCommentaryAsync_GeneratesAndSaves_WhenNoCache()
    {
        _commentaryRepo.Setup(c => c.GetCommentaryAsync("123")).ReturnsAsync((string?)null);
        var activity = MakeActivity();
        _repo.Setup(r => r.GetActivityAsync("123")).ReturnsAsync(activity);
        var points = new List<ActivityDataPoint> { new(0, 0, 100, null, null, null, null, null) };
        _repo.Setup(r => r.GetDataPointsAsync("123")).ReturnsAsync(points);
        _ollama.Setup(o => o.GenerateCommentaryAsync(activity, points, It.IsAny<CancellationToken>()))
            .ReturnsAsync("nouveau commentaire");

        var service = CreateService();
        var result = await service.GetOrGenerateCommentaryAsync("123");

        Assert.Equal("nouveau commentaire", result);
        _commentaryRepo.Verify(c => c.SaveCommentaryAsync("123", "nouveau commentaire", "test-model"), Times.Once);
    }

    [Fact]
    public async Task GetOrGenerateCommentaryAsync_ForceRegenerate_BypassesCache()
    {
        var activity = MakeActivity();
        _repo.Setup(r => r.GetActivityAsync("123")).ReturnsAsync(activity);
        _repo.Setup(r => r.GetDataPointsAsync("123")).ReturnsAsync([]);
        _ollama.Setup(o => o.GenerateCommentaryAsync(activity, It.IsAny<IEnumerable<ActivityDataPoint>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("régénéré");

        var service = CreateService();
        var result = await service.GetOrGenerateCommentaryAsync("123", forceRegenerate: true);

        Assert.Equal("régénéré", result);
        _commentaryRepo.Verify(c => c.GetCommentaryAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetActivitiesDashboardAsync_MarksIsDownloaded_BasedOnStoredIds()
    {
        _provider.Setup(p => p.GetValidAccessTokenAsync()).ReturnsAsync("token");
        _provider.Setup(p => p.GetActivitiesAsync("token")).ReturnsAsync([MakeActivity("1"), MakeActivity("2")]);
        _repo.Setup(r => r.GetStoredActivityIdsAsync()).ReturnsAsync(["1"]);

        var service = CreateService();
        var dashboard = await service.GetActivitiesDashboardAsync();

        Assert.True(dashboard.Single(a => a.Id == "1").IsDownloaded);
        Assert.False(dashboard.Single(a => a.Id == "2").IsDownloaded);
    }

    [Fact]
    public async Task GetLatestActivityCsvAsync_UsesFirstActivity_AndExportsCsv()
    {
        _provider.Setup(p => p.GetValidAccessTokenAsync()).ReturnsAsync("token");
        var latest = MakeActivity("1");
        _provider.Setup(p => p.GetActivitiesAsync("token")).ReturnsAsync([latest, MakeActivity("2")]);
        _repo.Setup(r => r.ExistsAsync("1")).ReturnsAsync(false);
        _provider.Setup(p => p.GetActivityDetailAsync("1", "token"))
            .ReturnsAsync(new ActivityDetail("1", 3.0, 5.0, null, null, null, null, null, [], []));
        var streams = new List<ActivityDataPoint> { new(0, 0, 100, null, null, null, null, null) };
        _provider.Setup(p => p.GetActivityStreamsAsync("1", "token")).ReturnsAsync(streams);
        _csvExportService.Setup(c => c.GetCsvBytes(streams)).Returns([1, 2, 3]);

        var service = CreateService();
        var (fileName, content) = await service.GetLatestActivityCsvAsync();

        Assert.Equal("activity_1.csv", fileName);
        Assert.Equal(new byte[] { 1, 2, 3 }, content);
    }
}
