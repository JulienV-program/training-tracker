namespace MyTracker.Infrastructure.Persistence.Entities;

public class ActivityEntity
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public DateTime StartDateLocal { get; set; }
    public string Type { get; set; } = string.Empty;
    public double Distance { get; set; }
    public int MovingTime { get; set; }
    public double ElevationGain { get; set; }
    public double? AverageHeartRate { get; set; }
    public double? MaxHeartRate { get; set; }
    public double? AverageWatts { get; set; }
    public double? MaxWatts { get; set; }
    public double? AverageCadence { get; set; }
    public double? Calories { get; set; }
    public double? SufferScore { get; set; }
    public double? AverageSpeed { get; set; }
    public double? MaxSpeed { get; set; }
    public double? StartLat { get; set; }
    public double? StartLng { get; set; }
    public double? EndLat { get; set; }
    public double? EndLng { get; set; }
    public string? MapPolyline { get; set; }
    public bool IsFullyImported { get; set; }
}
