using MyTracker.Domain.Models;

namespace MyTracker.Domain.Interfaces;

public interface ICsvExportService
{
    void ExportToCsv(IEnumerable<Activity> activities, string filePath);
    void ExportDetailedToCsv(IEnumerable<ActivityDataPoint> points, string filePath);
    byte[] GetCsvBytes<T>(IEnumerable<T> records);
}
