namespace MyTracker.Infrastructure.Persistence.Entities;

public class ActivitySplitEntity
{
    public int Id { get; set; }
    public string ActivityId { get; set; } = string.Empty;
    public int SplitIndex { get; set; }
    public double Distance { get; set; }
    public int ElapsedTime { get; set; }
    public double? ElevationDifference { get; set; }
    public int MovingTime { get; set; }
    public double AverageSpeed { get; set; }
    public double? AverageHeartRate { get; set; }
    public int? PaceZone { get; set; }
}
