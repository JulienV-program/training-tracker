using MyTracker.Domain.Models;

namespace MyTracker.Domain.Interfaces;

public interface IActivityRepository
{
    void ExportToCsv(IEnumerable<Activity> activities, string filePath);
    void ExportDetailedToCsv(IEnumerable<ActivityDataPoint> points, string filePath);
    byte[] GetCsvBytes<T>(IEnumerable<T> records);
    bool Exists(string activityId);
    byte[] Read(string activityId);
    void Save(string activityId, byte[] data);
    IEnumerable<string> GetStoredActivityIds();
}