using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MyTracker.Domain.Configurations;
using MyTracker.Domain.Exceptions;
using MyTracker.Domain.Interfaces;
using MyTracker.Domain.Models;
using MyTracker.Domain.Services;

namespace MyTracker.Domain.Tests;

public class CoachingServiceTests
{
    private readonly Mock<IActivityRepository> _repo = new();
    private readonly Mock<IActivityCommentaryRepository> _commentaryRepo = new();
    private readonly Mock<IUserProfileRepository> _userProfileRepo = new();
    private readonly Mock<IOllamaService> _ollama = new();

    private CoachingService CreateService() => new(
        _repo.Object, _commentaryRepo.Object, _userProfileRepo.Object, _ollama.Object,
        new OllamaSettings { Model = "test-model" }, NullLogger<CoachingService>.Instance);

    [Fact]
    public async Task ReturnsCached_WhenPresent()
    {
        _commentaryRepo.Setup(c => c.GetCommentaryAsync("123")).ReturnsAsync("commentaire en cache");

        var result = await CreateService().GetOrGenerateCommentaryAsync("123");

        Assert.Equal("commentaire en cache", result);
        _ollama.Verify(o => o.GenerateCommentaryAsync(It.IsAny<Activity>(), It.IsAny<IEnumerable<ActivityDataPoint>>(), It.IsAny<UserProfile?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GeneratesWithProfileAndSavesWithModelName_WhenNoCache()
    {
        var activity = TestData.Activity();
        var points = TestData.Points();
        var profile = new UserProfile(30, "Homme", 180, 75, 190, 55);
        _repo.Setup(r => r.GetActivityAsync("123")).ReturnsAsync(activity);
        _repo.Setup(r => r.GetDataPointsAsync("123")).ReturnsAsync(points);
        _userProfileRepo.Setup(p => p.GetProfileAsync()).ReturnsAsync(profile);
        _ollama.Setup(o => o.GenerateCommentaryAsync(activity, points, profile, It.IsAny<CancellationToken>()))
            .ReturnsAsync("nouveau commentaire");

        var result = await CreateService().GetOrGenerateCommentaryAsync("123");

        Assert.Equal("nouveau commentaire", result);
        _commentaryRepo.Verify(c => c.SaveCommentaryAsync("123", "nouveau commentaire", "test-model"), Times.Once);
    }

    [Fact]
    public async Task ForceRegenerate_BypassesCache()
    {
        var activity = TestData.Activity();
        _repo.Setup(r => r.GetActivityAsync("123")).ReturnsAsync(activity);
        _repo.Setup(r => r.GetDataPointsAsync("123")).ReturnsAsync([]);
        _ollama.Setup(o => o.GenerateCommentaryAsync(activity, It.IsAny<IEnumerable<ActivityDataPoint>>(), It.IsAny<UserProfile?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("régénéré");

        var result = await CreateService().GetOrGenerateCommentaryAsync("123", forceRegenerate: true);

        Assert.Equal("régénéré", result);
        _commentaryRepo.Verify(c => c.GetCommentaryAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ThrowsActivityNotFound_WhenActivityNotInCache()
    {
        await Assert.ThrowsAsync<ActivityNotFoundException>(() => CreateService().GetOrGenerateCommentaryAsync("404"));
    }
}
