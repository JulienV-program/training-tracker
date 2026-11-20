using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MyTracker.Domain.Interfaces;
using MyTracker.Domain.Models;
using MyTracker.Domain.Services;

namespace MyTracker.Domain.Tests;

public class ActivityExportServiceTests
{
    [Fact]
    public async Task GetLatestActivityCsvAsync_ImportsLatestActivity_AndExportsCsv()
    {
        var provider = new Mock<IActivityProvider>();
        var repo = new Mock<IActivityRepository>();
        var csv = new Mock<ICsvExportService>();
        provider.Setup(p => p.GetValidAccessTokenAsync()).ReturnsAsync("token");
        provider.Setup(p => p.GetActivitiesAsync("token")).ReturnsAsync([TestData.Activity("1")]);
        provider.Setup(p => p.GetActivityDetailAsync("1", "token")).ReturnsAsync(TestData.Detail("1"));
        provider.Setup(p => p.GetActivityStreamsAsync("1", "token")).ReturnsAsync(TestData.Points());
        csv.Setup(c => c.GetCsvBytes(It.IsAny<IEnumerable<ActivityDataPoint>>())).Returns([1, 2, 3]);

        var importer = new ActivityImportService(provider.Object, repo.Object, NullLogger<ActivityImportService>.Instance);
        var (fileName, content) = await new ActivityExportService(importer, csv.Object).GetLatestActivityCsvAsync();

        Assert.Equal("activity_1.csv", fileName);
        Assert.Equal(new byte[] { 1, 2, 3 }, content);
    }
}
