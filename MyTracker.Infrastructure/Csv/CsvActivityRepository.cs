using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using MyTracker.Domain.Interfaces;
using MyTracker.Domain.Models;

namespace MyTracker.Infrastructure.Csv;

public class CsvActivityRepository : IActivityRepository
{
    private readonly string _storagePath = "exports";
    
    public CsvActivityRepository()
    {
        if (!Directory.Exists(_storagePath)) Directory.CreateDirectory(_storagePath);
    }

    public bool Exists(string activityId) => File.Exists(Path.Combine(_storagePath, $"activity_{activityId}.csv"));

    public byte[] Read(string activityId) => File.ReadAllBytes(Path.Combine(_storagePath, $"activity_{activityId}.csv"));

    public void Save(string activityId, byte[] data) => File.WriteAllBytes(Path.Combine(_storagePath, $"activity_{activityId}.csv"), data);

    public void ExportToCsv(IEnumerable<Activity> activities, string filePath)
    {
        using var writer = new StreamWriter(filePath);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        
        // On écrit les données
        csv.WriteRecords(activities);
    }

    public void ExportDetailedToCsv(IEnumerable<ActivityDataPoint> points, string filePath)
    {
        using var writer = new StreamWriter(filePath);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        
        // On écrit manuellement le header pour être ultra-clair pour l'IA
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

    public IEnumerable<string> GetStoredActivityIds()
    {
        if (!Directory.Exists(_storagePath)) return Enumerable.Empty<string>();

        // On récupère les fichiers, on extrait l'ID du nom "activity_123.csv"
        return Directory.GetFiles(_storagePath, "activity_*.csv")
                        .Select(Path.GetFileNameWithoutExtension)
                        .Select(name => name!.Replace("activity_", ""))
                        .ToList();
    }
}