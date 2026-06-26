namespace MyTracker.Infrastructure.Persistence.Entities;

public class ActivityLapEntity
{
    public int Id { get; set; }
    public string ActivityId { get; set; } = string.Empty;
    public long StravaLapId { get; set; }
    public int LapIndex { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ElapsedTime { get; set; }
    public int MovingTime { get; set; }
    public DateTime StartDate { get; set; }
    public double Distance { get; set; }
    public double AverageSpeed { get; set; }
    public double? MaxSpeed { get; set; }
    public double? AverageHeartRate { get; set; }
    public double? MaxHeartRate { get; set; }
    public double? ElevationGain { get; set; }
}
