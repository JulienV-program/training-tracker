using System.Text;
using MyTracker.Domain.Models;
using MyTracker.Infrastructure.Csv;
using Xunit;

namespace MyTracker.Infrastructure.Tests;

public class CsvExportServiceTests
{
    [Fact]
    public void GetCsvBytes_ProducesHeaderAndOneRowPerDataPoint()
    {
        var points = new List<ActivityDataPoint>
        {
            new(0, 0, 100, null, null, 50, null, 2.5),
            new(1, 5, 105, null, null, 51, null, 2.6)
        };

        var service = new CsvExportService();
        var csv = Encoding.UTF8.GetString(service.GetCsvBytes(points));

        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        Assert.Equal(3, lines.Length); // header + 2 rows
        Assert.Contains("TimeOffset", lines[0]);
        Assert.Contains("HeartRate", lines[0]);
        Assert.Contains("100", lines[1]);
        Assert.Contains("105", lines[2]);
    }
}
