using System.Globalization;
using CsvHelper;
using MyTracker.Domain.Interfaces;
using MyTracker.Domain.Models;

namespace MyTracker.Infrastructure.Csv;

public class CsvExportService : ICsvExportService
{
    public void ExportToCsv(IEnumerable<Activity> activities, string filePath)
    {
        using var writer = new StreamWriter(filePath);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csv.WriteRecords(activities);
    }

    public void ExportDetailedToCsv(IEnumerable<ActivityDataPoint> points, string filePath)
    {
        using var writer = new StreamWriter(filePath);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        csv.WriteHeader<ActivityDataPoint>();
        csv.NextRecord();
        csv.WriteRecords(points);
    }

    public byte[] GetCsvBytes<T>(IEnumerable<T> records)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        csv.WriteRecords(records);
        writer.Flush();
        return memoryStream.ToArray();
    }
}
